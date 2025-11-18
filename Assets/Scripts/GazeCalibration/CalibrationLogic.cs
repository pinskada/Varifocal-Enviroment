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

    // Overview of calibration control messages:
    // MessageType.gazeCalcControl  ... controls the calibration state in the RPI
    // RouteQueueContainer.routeQueue.Add((message, MessageType.gazeCalcControl));   ... sends the message to RPI

    public void Start()
    {
        CheckComponents();
        ToggleGazeTarget(false);
        ToggleText(true);
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

        // Sends "start_gaze_preview" command to RPI
        RouteQueueContainer.routeQueue.Add((new { command = "start_gaze_preview" }, MessageType.gazeCalcControl));

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

    public void SetTargetDistance(float distance)
    {
        if (distance < 0.1f)
        {
            distance = 0.1f; // Minimum distance limit
            Debug.LogWarning("Gaze target distance too small, setting to minimum of 0.1m.");
        }
        GazeTarget.transform.position = CameraTarget.transform.position + CameraTarget.transform.forward * distance;
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

        foreach (var entry in settings.calibrationPoints)
        {
            SetTargetDistance(entry.distance);

            // START marker
            RouteQueueContainer.routeQueue.Add((new { state = "start", distance = entry.distance }, MessageType.gazeCalcControl));

            yield return new WaitForSeconds(entry.holdTime);

            // STOP marker
            RouteQueueContainer.routeQueue.Add((new { state = "stop", distance = entry.distance }, MessageType.gazeCalcControl));

            yield return new WaitForSeconds(settings.pauseBetweenPoints);
        }
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

}