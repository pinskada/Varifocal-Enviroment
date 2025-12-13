using UnityEngine;
using Contracts;
using System.Collections;

public class CalibrationLogic : MonoBehaviour, ICalibrationHub
{
    [SerializeField] private GameObject CameraTarget;
    [SerializeField] private GameObject GazeTarget;
    [SerializeField] private GameObject InstructionText;

    private Coroutine currentRoutine;
    private bool inPreviewMode = false;

    private UnityEngine.Vector3 targetOffset = new UnityEngine.Vector3(0f, 0.065f, 0f);



    public void Start()
    {
        CheckComponents();
        ToggleGazeTarget(false);
        ToggleText(true);
    }


    public void Update()
    {
        var hasUpdate = TargetDistanceQueueContainer.TargetDistanceQueue.TryDequeue(out var distance);

        if (hasUpdate && inPreviewMode)
        {
            SetTargetDistance(new TargetPosition { distance = distance, horizontal = 0f, vertical = 0f });
            Debug.Log($"[CalibrationLogic] Updated gaze target distance to {distance}m in preview mode.");
        }
        else if (hasUpdate && !inPreviewMode)
        {
            SetCalibState(CalibState.GazePreview);
            SetTargetDistance(new TargetPosition { distance = distance, horizontal = 0f, vertical = 0f });
            Debug.Log($"[CalibrationLogic] Entered preview mode and set gaze target distance to {distance}m.");
        }

        // Cycle to previous scene on Left Arrow
        if (Input.GetKeyDown(KeyCode.A))
        {
            SetCalibState(CalibState.Calibration);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            SetCalibState(CalibState.GazeMeasure);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            SetCalibState(CalibState.GazePreview);
        }
    }


    public void SetCalibState(CalibState state)
    {
        if (currentRoutine != null) { StopCoroutine(currentRoutine); }
        if (inPreviewMode)
        {
            ExitPreviewMode();
            inPreviewMode = false;
        }

        switch (state)
        {
            case CalibState.Calibration:
                currentRoutine = StartCoroutine(RunCalibrationSequence());
                break;
            case CalibState.GazeMeasure:
                currentRoutine = StartCoroutine(RunGazeMeasureSequence());
                break;
            case CalibState.GazePreview:
                inPreviewMode = true;
                EnterPreviewMode();
                break;
        }
    }


    private IEnumerator RunCalibrationSequence()
    {
        // Turns off InstructionText
        ToggleText(false);
        // Turns on GazeTarget
        ToggleGazeTarget(true);

        // Sends "start_calibration" command to RPI
        RouteQueueContainer.routeQueue.Add((new { command = "start_calibration" }, MessageType.gazeCalcControl));

        // Calls CycleGazePoint coroutine
        yield return StartCoroutine(CycleGazePoint());

        // Sends "end_calibration" command to RPI
        RouteQueueContainer.routeQueue.Add((new { command = "end_calibration" }, MessageType.gazeCalcControl));

        // Turns off GazeTarget
        ToggleGazeTarget(false);
        // Turns on InstructionText
        ToggleText(true);

        yield return null;
    }


    private IEnumerator RunGazeMeasureSequence()
    {
        // Turns off InstructionText
        ToggleText(false);
        // Turns on GazeTarget
        ToggleGazeTarget(true);

        // Sends "start_gaze_measure" command to RPI
        RouteQueueContainer.routeQueue.Add((new { command = "start_gaze_measure" }, MessageType.gazeCalcControl));

        // Calls CycleGazePoint coroutine
        yield return StartCoroutine(CycleGazePoint());

        // Sends "end_gaze_measure" command to RPI
        RouteQueueContainer.routeQueue.Add((new { command = "end_gaze_measure" }, MessageType.gazeCalcControl));

        // Turns off GazeTarget
        ToggleGazeTarget(false);
        // Turns on InstructionText
        ToggleText(true);

        yield return null;
    }


    private void EnterPreviewMode()
    {
        // Turns off InstructionText
        ToggleText(false);
        // Turns on GazeTarget
        ToggleGazeTarget(true);

        // SetTargetDistance() gets called from GUI with user defined distances
    }


    private void ExitPreviewMode()
    {
        // Sends "end_gaze_preview" command to RPI
        RouteQueueContainer.routeQueue.Add((new { command = "end_gaze_preview" }, MessageType.gazeCalcControl));

        // Turns off GazeTarget
        ToggleGazeTarget(false);
        // Turns on InstructionText
        ToggleText(true);
    }


    public void SetTargetDistance(TargetPosition target_position)
    {
        // Distance in meters
        float distance = target_position.distance;
        float horizontalAngle = target_position.horizontal; // degrees, + = to the right
        float verticalAngle = target_position.vertical; // degrees, + = up

        // Clamp distance to a reasonable minimum
        if (distance < 0.1f)
        {
            distance = 0.1f;
            Debug.LogWarning("Gaze target distance too small, setting to minimum of 0.1m.");
        }

        // Start from camera's forward direction
        Vector3 direction = CameraTarget.transform.forward;

        // Rotate around camera's UP for horizontal angle (yaw)
        direction = Quaternion.AngleAxis(horizontalAngle, CameraTarget.transform.up) * direction;

        // Rotate around camera's RIGHT for vertical angle (pitch)
        // If you find "up" and "down" flipped, just negate verticalAngle here.
        direction = Quaternion.AngleAxis(-verticalAngle, CameraTarget.transform.right) * direction;

        // Set world-space position at the desired distance
        GazeTarget.transform.position =
            CameraTarget.transform.position + direction.normalized * distance + targetOffset;
    }


    private void ToggleText(bool display)
    {
        InstructionText.SetActive(display);
    }


    private void ToggleGazeTarget(bool display)
    {
        GazeTarget.SetActive(display);
    }


    private IEnumerator CycleGazePoint()
    {
        // No need for mode argument, this will cycle the points same for all modes

        // for curr_distance, curr_wait_time in Settings.calibration_points:
        //      SetTargetDistance(point)
        //      Sends message with dict: "state": start; "distance": curr_distance
        //      Wait for curr_wait_time seconds
        //      Sends message with dict: "state": stop; "distance": curr_distance
        //      Waits 0,5 seconds between points
        var settings = Settings.calibrationSettings;
        var holdPointTime = settings.holdPointTime;
        var pauseBetweenPoints = settings.pauseBetweenPoints;

        yield return new WaitForSeconds(pauseBetweenPoints);

        foreach (var entry in settings.calibrationPoints)
        {
            Debug.Log($"Calibrating point ID {entry.id}");
            SetTargetDistance(entry.target_position);
            ToggleGazeTarget(true);

            // START marker
            SendSceneMarker(MarkerState.START, entry);

            yield return new WaitForSeconds(holdPointTime);

            // STOP marker
            SendSceneMarker(MarkerState.STOP, entry);

            ToggleGazeTarget(false);
            yield return new WaitForSeconds(pauseBetweenPoints);
        }
    }


    private void SendSceneMarker(MarkerState state, CalibrationPoint entry)
    {
        // Build strongly-typed SceneMarker first (nice for debugging / future use)
        var marker = new SceneMarker
        {
            id = entry.id,
            state = state,
            type = entry.type,
            target_position = entry.target_position
        };

        // DTO for JSON – ensures enums go out as strings
        var dto = new
        {
            id = marker.id,
            state = marker.state.ToString(),   // "START" / "STOP"
            type = marker.type.ToString(),     // "REF" / "DIST" / "ANG"
            target_position = new
            {
                distance = marker.target_position.distance,
                horizontal = marker.target_position.horizontal,
                vertical = marker.target_position.vertical
            }
        };

        // Send via your existing route queue
        RouteQueueContainer.routeQueue.Add((dto, MessageType.sceneMarker));
    }


    private void CheckComponents()
    {
        if (CameraTarget == null)
        {
            Debug.LogError("CameraTarget is not assigned in CalibrationLogic.");
        }
        if (GazeTarget == null)
        {
            Debug.LogError("GazeTarget is not assigned in CalibrationLogic.");
        }
        if (InstructionText == null)
        {
            Debug.LogError("InstructionText is not assigned in CalibrationLogic.");
        }
    }

    public void SettingsChanged(string moduleName, string fieldName)
    {
        if (fieldName == "targetPreviewDistance")
        {
            Debug.Log("[CalibrationLogic] Gaze target preview distance changed in settings.");
            SetTargetDistance(new TargetPosition { distance = Settings.gazeCalculator.targetPreviewDistance, horizontal = 0f, vertical = 0f });
        }
    }

}
