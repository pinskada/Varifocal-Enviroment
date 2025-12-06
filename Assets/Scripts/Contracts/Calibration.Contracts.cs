
namespace Contracts
{
    public enum CalibState
    {
        Calibration, // Full calibration proces
        GazeMeasure, // Calib scene displays points, RPI send calculated gaze distances
        GazePreview,  // Calib scene shows user defined gaze points, RPI sends calculated gaze distance
    }

    [System.Serializable]
    public enum MarkerState
    {
        START = 0,
        STOP = 1,
    }

    [System.Serializable]
    public enum MarkerType
    {
        REF = 0,
        DIST = 1,
        ANG = 2,
    }

    [System.Serializable]
    public class TargetPosition
    {
        public float distance;
        public float horizontal;
        public float vertical;
    }

    [System.Serializable]
    public class SceneMarker
    {
        public int id;
        public MarkerState state;
        public MarkerType type;
        public TargetPosition target_position;
    }

    public class CalibrationPoint
    {
        public int id;
        public MarkerType type;
        public TargetPosition target_position;
    }

    public interface ICalibrationHub
    {
        // Sets the current calibration state
        public void SetCalibState(CalibState state);

    }
}