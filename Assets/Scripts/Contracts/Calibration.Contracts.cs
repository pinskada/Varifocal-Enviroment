using UnityEngine;

namespace Contracts
{
    public enum CalibState
    {
        Calibration, // Full calibration proces
        GazeMeasure, // Calib scene displays points, RPI send calculated gaze distances
        GazePreview,  // Calib scene shows user defined gaze points, RPI sends calculated gaze distance
    }

    public interface ICalibrationHub
    {
        // Sets the current calibration state
        public void SetCalibState(CalibState state);

    }
}