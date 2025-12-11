using UnityEngine;
using Contracts;
using System.Threading;

public class GazeDistanceCalculator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] GameObject cameraObject;
    private volatile CalibratedData calibratedData;
    private bool isCalibrated = false;
    private Thread gazeCalculatorThread;
    private Thread calibratorThread;
    private volatile bool runRaycast = false;
    private volatile bool newGazeAvailable = false;
    private volatile float vergenceDistance = 0f;
    private volatile float cyclopeanYawDeg = 0f;   // horizontal gaze angle (deg)
    private volatile float cyclopeanPitchDeg = 0f; // vertical gaze angle (deg)


    void Start()
    {
        if (cameraObject == null)
        {
            Debug.LogError("[GazeDistanceCalculator] No camera object assigned!");
        }

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

        // Debug.Log($"Calib ref_left: {calibratedData.reference.left_ref[0]}, {calibratedData.reference.left_ref[1]}.");
        // Debug.Log($"Calib ref_right: {calibratedData.reference.right_ref[0]}, {calibratedData.reference.right_ref[1]}.");
        // Debug.Log($"Calib angle_left: {string.Join(", ", calibratedData.angle.left.fx.coeffs)}, {string.Join(", ", calibratedData.angle.left.fy.coeffs)}.");
        // Debug.Log($"Calib angle_right: {string.Join(", ", calibratedData.angle.right.fx.coeffs)}, {string.Join(", ", calibratedData.angle.right.fy.coeffs)}.");
        // Debug.Log($"Calib distance: a={calibratedData.distance.a}, b={calibratedData.distance.b}.");
    }


    private void DequeueEyeVectors()
    {
        foreach (EyeVectors data in EyeVectorsQueueContainer.EyeVectorsQueue.GetConsumingEnumerable())
        {
            // Debug.Log($"Left - {data.left_eye_vector.dx}, {data.left_eye_vector.dy}; Right - {data.right_eye_vector.dx}, {data.right_eye_vector.dy}");
            if (calibratedData != null)
            {
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
        var useTracker = Settings.gazeCalculator.useTracker;
        if (useTracker == 0)
        {
            runRaycast = true;
        }

        // Skip update if not calibrated yet
        if (!isCalibrated)
            return;
        // Skip update if no new eyeVectors since last frame
        if (!newGazeAvailable && useTracker == 1)
            return;

        float finalDistance;
        // Optionally, you can call calculation methods here if they need to run on the main thread
        if (runRaycast)
        {
            var rayCastDistance = CalculateRayCastDistance();
            finalDistance = rayCastDistance;
        }
        else
        {
            finalDistance = vergenceDistance;
        }
        // Reset flag: we consumed the new value
        newGazeAvailable = false;
        Debug.Log($"Final: {finalDistance}, Vergence: {vergenceDistance}, Raycast: {runRaycast}");
        RouteQueueContainer.routeQueue.Add((finalDistance, MessageType.gazeData));
    }


    // Raycast-based distance estimation for far objects.
    // Currently uses camera forward as a proxy for gaze direction.
    // You can replace this with a more accurate gaze-based ray later.
    private float CalculateRayCastDistance()
    {
        var useTracker = Settings.gazeCalculator.useTracker;

        Vector3 origin = cameraObject.transform.position;
        Vector3 direction;

        if (useTracker == 0)
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

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            return hit.distance;
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
}
