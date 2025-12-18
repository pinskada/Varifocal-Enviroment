using UnityEngine;
using Contracts;
using System.Threading;

public class GazeDistanceCalculator : MonoBehaviour, IModuleSettingsHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] GameObject cameraObject;
    private volatile CalibratedData calibratedData;
    private bool isCalibrated = false;
    private bool isTcpReady = false;
    private Thread gazeCalculatorThread;
    private Thread calibratorThread;
    private volatile bool runRaycast = false;
    private volatile bool newGazeAvailable = false;
    private volatile float vergenceDistance = 0f;
    private volatile float cyclopeanYawDeg = 0f;   // horizontal gaze angle (deg)
    private volatile float cyclopeanPitchDeg = 0f; // vertical gaze angle (deg)
    private LineRenderer leftRayRenderer;
    private LineRenderer rightRayRenderer;
    private float debugRayLength = 50f;
    private volatile float leftYawDeg = 0f;
    private volatile float leftPitchDeg = 0f;
    private volatile float rightYawDeg = 0f;
    private volatile float rightPitchDeg = 0f;
    private float lastSentDistance = -1f; // invalid = never sent
    private float lastSendTime = -999f;
    [SerializeField] private LayerMask raycastMask = ~0;

    void OnEnable()
    {
        CommEvents.TcpConnected += OnTcpReady;
        CommEvents.TcpDisconnected += OnTcpLost;
    }

    void OnDisable()
    {
        CommEvents.TcpConnected -= OnTcpReady;
        CommEvents.TcpDisconnected -= OnTcpLost;
    }

    void OnTcpReady() => isTcpReady = true;
    void OnTcpLost() => isTcpReady = false;


    void Start()
    {
        if (cameraObject == null)
        {
            Debug.LogError("[GazeDistanceCalculator] No camera object assigned!");
            return;
        }

        CreateLineRenderers();

        calibratorThread = new Thread(DequeueCalibrators)
        {
            IsBackground = true,
            Name = "GazeDistCal.CalibratorDequeue"
        };
        calibratorThread.Start();

        gazeCalculatorThread = new Thread(DequeueEyeVectors)
        {
            IsBackground = true,
            Name = "GazeDistCal.EyeVectorsDequeue"
        };
        gazeCalculatorThread.Start();
    }

    public void SettingsChanged(string moduleName, string fieldName)
    {
        // This method is called when settings are changed in the ConfigManager.
        // You can implement any necessary actions to handle the updated settings here.
        if (fieldName == "manualDistanceValue")
        {
            var manualDistance = Settings.gazeCalculator.manualDistanceValue;
            RouteQueueContainer.routeQueue.Add((manualDistance, MessageType.gazeData));
        }
    }

    void OnApplicationQuit()
    {
        EyeVectorsQueueContainer.EyeVectorsQueue.CompleteAdding();
        CalibratorQueueContainer.CalibratorQueue.CompleteAdding();
        if (gazeCalculatorThread != null && gazeCalculatorThread.IsAlive)
        {
            gazeCalculatorThread.Join(1000);
        }
        if (calibratorThread != null && calibratorThread.IsAlive)
        {
            calibratorThread.Join(1000);
        }
    }


    private void DequeueCalibrators()
    {
        foreach (CalibratedData data in CalibratorQueueContainer.CalibratorQueue.GetConsumingEnumerable())
        {
            SetCalibParam(data);
        }
    }


    public void SetCalibParam(CalibratedData data)
    {
        calibratedData = data;
        isCalibrated = true;
        RouteQueueContainer.routeQueue.Add((new { command = "start_gaze_calc" }, MessageType.gazeCalcControl));
        Debug.Log("Received new calibration data in GazeDistanceCalculator.");
    }


    private void DequeueEyeVectors()
    {
        foreach (EyeVectors data in EyeVectorsQueueContainer.EyeVectorsQueue.GetConsumingEnumerable())
        {
            // Debug.Log($"Left - {data.left_eye_vector.dx}, {data.left_eye_vector.dy}; Right - {data.right_eye_vector.dx}, {data.right_eye_vector.dy}");
            if (isCalibrated == true)
            {
                Debug.Log("GazeDistanceCalculator received new EyeVectors.");
                CalculateData(data);
            }
        }
    }


    private void CalculateData(EyeVectors eyeVectors)
    {
        var calib = calibratedData;
        if (calib == null) return;

        // 1) Subtract reference
        var referenceVectors = CalculateReferenceVectors(eyeVectors, calib.reference);

        // 2) Map deltas to angles (deg) using fitted polynomials
        var (leftAngles, rightAngles) = CalculateAngles(referenceVectors, calib.angle);

        // store per-eye gaze angles for debug visualization
        if (Settings.gazeCalculator.useTracker == true)
        {
            leftYawDeg = leftAngles.x;
            leftPitchDeg = leftAngles.y;
            rightYawDeg = rightAngles.x;
            rightPitchDeg = rightAngles.y;
        }

        // 3) Compute vergence-based distance (m)
        var distance = CalculateVergenceDistance(leftAngles, rightAngles, calib.distance);

        // 4) Compute cyclopean gaze angles (deg)
        cyclopeanYawDeg = 0.5f * (leftAngles.x + rightAngles.x); // horizontal
        cyclopeanPitchDeg = 0.5f * (leftAngles.y + rightAngles.y); // vertical

        vergenceDistance = distance;
        // Debug.Log($"Calculated vergence distance: {distance} meters");

        runRaycast = !float.IsFinite(distance) || distance > 2.0f;

        newGazeAvailable = true;
    }


    // Subtract per-eye reference vectors (infinite-distance baseline).
    // eyeVectors and reference are in the same units (pixels in your pipeline).
    // Returns deltas relative to reference.
    private EyeVectors CalculateReferenceVectors(EyeVectors eyeVectors, ReferenceParams reference)
    {
        EyeVectors result = new EyeVectors
        {
            left_eye_vector = new EyeVector
            {
                dx = eyeVectors.left_eye_vector.dx - reference.left_ref[0],
                dy = eyeVectors.left_eye_vector.dy - reference.left_ref[1]
            },
            right_eye_vector = new EyeVector
            {
                dx = eyeVectors.right_eye_vector.dx - reference.right_ref[0],
                dy = eyeVectors.right_eye_vector.dy - reference.right_ref[1]
            }
        };
        return result;
    }


    private (Vector2 leftAngles, Vector2 rightAngles) CalculateAngles(
        EyeVectors referenceVectors,
        AngleParams angleParams
    )
    {
        Vector2 leftDelta = new Vector2(referenceVectors.left_eye_vector.dx, referenceVectors.left_eye_vector.dy);
        Vector2 rightDelta = new Vector2(referenceVectors.right_eye_vector.dx, referenceVectors.right_eye_vector.dy);

        Vector2 leftAngles = Vector2.zero;
        Vector2 rightAngles = Vector2.zero;

        // Left eye: horizontal (x), vertical (y)
        if (angleParams.left != null)
        {
            if (angleParams.left.fx != null && angleParams.left.fx.coeffs != null)
            {
                leftAngles.x = EvalPoly(angleParams.left.fx.coeffs, leftDelta.x);
            }
            if (angleParams.left.fy != null && angleParams.left.fy.coeffs != null)
            {
                leftAngles.y = EvalPoly(angleParams.left.fy.coeffs, leftDelta.y);
            }
        }

        // Right eye: horizontal (x), vertical (y)
        if (angleParams.right != null)
        {
            if (angleParams.right.fx != null && angleParams.right.fx.coeffs != null)
            {
                rightAngles.x = EvalPoly(angleParams.right.fx.coeffs, rightDelta.x);
            }
            if (angleParams.right.fy != null && angleParams.right.fy.coeffs != null)
            {
                rightAngles.y = EvalPoly(angleParams.right.fy.coeffs, rightDelta.y);
            }
        }

        return (leftAngles, rightAngles);
    }



    // Compute vergence-based distance using the distance calibration.
    // leftAngles/rightAngles.x are horizontal angles in degrees for each eye.
    // DistanceParams implement: distance ≈ a * (1 / vergence_rad) + b.
    private float CalculateVergenceDistance(
        Vector2 leftAngles,
        Vector2 rightAngles,
        DistanceParams distanceParams)
    {
        // Horizontal vergence in degrees
        float vergenceDeg = Mathf.Abs(leftAngles.x - rightAngles.x);
        float vergenceRad = vergenceDeg * Mathf.Deg2Rad;

        // Very small vergence → effectively infinite distance
        const float epsilon = 1e-6f;
        if (vergenceRad <= epsilon)
        {
            return float.PositiveInfinity;
        }

        float z = 1.0f / vergenceRad;
        float a = distanceParams != null ? distanceParams.a : 0f;
        float b = distanceParams != null ? distanceParams.b : 0f;

        float d = a * z + b; // meters

        // Clamp to non-negative distance
        if (d < 0f) d = 0f;

        return d;
    }


    void Update()
    {
        SetRaysVisibility();

        if (!isTcpReady)
            return;

        if (Settings.gazeCalculator.manualDistanceMode)
            return;

        var useTracker = Settings.gazeCalculator.useTracker;

        if (!isCalibrated && useTracker == true)
            return;

        if (!newGazeAvailable && useTracker == true)
            return;

        float finalDistance;
        if (!runRaycast && useTracker)
            finalDistance = vergenceDistance;
        else
            finalDistance = CalculateRayCastDistance();

        newGazeAvailable = false;

        // Debug.Log(finalDistance);
        if (ShouldSendDistance(finalDistance))
        {
            lastSentDistance = finalDistance;
            RouteQueueContainer.routeQueue.Add((finalDistance, MessageType.gazeData));
        }

        if (Settings.gazeCalculator.drawRays)
            DrawGazeRays();
    }


    private bool ShouldSendDistance(float newDistance)
    {
        if (Time.time - lastSendTime < Settings.gazeCalculator.minSendInterval)
            return false;

        if (newDistance <= 0.0f)
            return false;

        lastSendTime = Time.time;
        // Always send first valid value
        if (lastSentDistance <= 0f)
            return true;

        // Reject non-finite values (optional safety)
        if (!float.IsFinite(newDistance))
            return false;

        float ratio = newDistance / lastSentDistance;

        return ratio >= Settings.gazeCalculator.distanceChangeRatio || ratio <= (1f / Settings.gazeCalculator.distanceChangeRatio);
    }

    private void SetRaysVisibility()
    {
        if (leftRayRenderer != null)
            leftRayRenderer.enabled = Settings.gazeCalculator.drawRays;

        if (rightRayRenderer != null)
            rightRayRenderer.enabled = Settings.gazeCalculator.drawRays;
    }

    // Raycast-based distance estimation for far objects.
    // Currently uses camera forward as a proxy for gaze direction.
    // You can replace this with a more accurate gaze-based ray later.
    private float CalculateRayCastDistance()
    {
        var useTracker = Settings.gazeCalculator.useTracker;

        Vector3 direction;

        if (useTracker == false)
        {
            // Eye tracker disabled → always raycast straight forward
            direction = cameraObject.transform.forward;
        }
        else
        {
            // Eye tracker enabled → use cyclopean gaze angles
            float yawDeg = cyclopeanYawDeg;    // horizontal angle
            float pitchDeg = cyclopeanPitchDeg;  // vertical angle

            // Local rotation from forward by yaw (Y) and pitch (X).
            Quaternion localRot = Quaternion.Euler(-pitchDeg, yawDeg, 0f);

            // Transform this local gaze direction into world space
            direction = cameraObject.transform.TransformDirection(localRot * Vector3.forward);
        }

        float maxDistance = Settings.gazeCalculator.distanceThreshold; // 2.0f meters
        Vector3 origin = cameraObject.transform.position + direction * 0.05f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(hit.distance, 0.2f);
        }

        // If nothing is hit, treat as max distance
        return maxDistance;
    }


    // Evaluate polynomial with coeffs in numpy.polyfit order (highest degree first).
    // Uses Horner's method: (((c0 * x + c1) * x + c2) * x + ...).
    private static float EvalPoly(float[] coeffs, float x)
    {
        if (coeffs == null || coeffs.Length == 0)
            return 0f;

        float result = 0f;
        for (int i = 0; i < coeffs.Length; i++)
        {
            result = result * x + coeffs[i];
        }
        return result;
    }

    private void CreateLineRenderers()
    {
        var goL = new GameObject("LeftGazeRay");
        goL.transform.SetParent(cameraObject.transform, false);
        leftRayRenderer = goL.AddComponent<LineRenderer>();
        leftRayRenderer.positionCount = 2;
        leftRayRenderer.startWidth = 0.002f;
        leftRayRenderer.endWidth = 0.005f;
        leftRayRenderer.useWorldSpace = true;

        var goR = new GameObject("RightGazeRay");
        goR.transform.SetParent(cameraObject.transform, false);
        rightRayRenderer = goR.AddComponent<LineRenderer>();
        rightRayRenderer.positionCount = 2;
        rightRayRenderer.startWidth = 0.002f;
        rightRayRenderer.endWidth = 0.005f;
        rightRayRenderer.useWorldSpace = true;
    }

    private void DrawGazeRays()
    {
        if (cameraObject == null)
            return;
        if (Settings.gazeCalculator.numberOfRays == 2)
        {
            // Half IPD offset
            float halfIpd = Settings.display.ipd * 0.0005f;

            // camera's local horizontal axis in world space
            Vector3 right = cameraObject.transform.right;
            Vector3 up = cameraObject.transform.up;

            Vector3 originL = cameraObject.transform.position - right * halfIpd * 2;
            Vector3 originR = cameraObject.transform.position + right * halfIpd * 2;

            // If you don't want rays when tracker is disabled, early out:
            if (Settings.gazeCalculator.useTracker == false)
            {
                // Optionally still show straight-forward rays:
                UpdateLineRenderer(leftRayRenderer, originL,
                    cameraObject.transform.forward * debugRayLength);
                UpdateLineRenderer(rightRayRenderer, originR,
                    cameraObject.transform.forward * debugRayLength);
                // Debug.Log($"Left Ray - Yaw: {leftYawDeg}, Pitch: {leftPitchDeg}; Right Ray - Yaw: {rightYawDeg}, Pitch: {rightPitchDeg}");

                return;
            }

            Vector3 dirL = GetWorldGazeDirection(cameraObject.transform, leftYawDeg, leftPitchDeg);
            Vector3 dirR = GetWorldGazeDirection(cameraObject.transform, rightYawDeg, rightPitchDeg);

            UpdateLineRenderer(leftRayRenderer, originL, dirL * debugRayLength);
            UpdateLineRenderer(rightRayRenderer, originR, dirR * debugRayLength);

            // Debug.Log($"Left Ray - Yaw: {leftYawDeg}, Pitch: {leftPitchDeg}; Right Ray - Yaw: {rightYawDeg}, Pitch: {rightPitchDeg}");
        }
        else if (Settings.gazeCalculator.numberOfRays == 1)
        {
            Vector3 origin = cameraObject.transform.position;

            // If you don't want rays when tracker is disabled, early out:
            if (Settings.gazeCalculator.useTracker == false)
            {
                // Optionally still show straight-forward ray:
                UpdateLineRenderer(leftRayRenderer, origin,
                    cameraObject.transform.forward * debugRayLength);
                // Debug.Log($"Cyclopean Ray - Yaw: {cyclopeanYawDeg}, Pitch: {cyclopeanPitchDeg}");

                return;
            }

            Vector3 dir = GetWorldGazeDirection(cameraObject.transform, cyclopeanYawDeg, cyclopeanPitchDeg);

            UpdateLineRenderer(leftRayRenderer, origin, dir * debugRayLength);

            // Debug.Log($"Cyclopean Ray - Yaw: {cyclopeanYawDeg}, Pitch: {cyclopeanPitchDeg}");
        }
    }

    private static Vector3 GetWorldGazeDirection(Transform origin, float yawDeg, float pitchDeg)
    {
        // Local rotation from forward by yaw (Y) and pitch (X)
        Quaternion localRot = Quaternion.Euler(-pitchDeg, yawDeg, 0f);
        return origin.TransformDirection(localRot * Vector3.forward);
    }

    private static void UpdateLineRenderer(LineRenderer lr, Vector3 origin, Vector3 offset)
    {
        if (lr == null)
            return;

        lr.positionCount = 2;
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + offset);
    }

}
