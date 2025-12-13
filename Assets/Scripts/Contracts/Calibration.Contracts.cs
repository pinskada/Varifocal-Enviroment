using System.Collections.Concurrent;
using UnityEngine;

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

    [System.Serializable]
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



    [System.Serializable]
    public class AngleFitFunction
    {
        public float[] coeffs; // same order as numpy.polyfit (highest degree first)
    }

    [System.Serializable]
    public class AngleParamsPerEye
    {
        public AngleFitFunction fx;
        public AngleFitFunction fy;
    }

    [System.Serializable]
    public class AngleParams
    {
        public AngleParamsPerEye left;
        public AngleParamsPerEye right;
    }

    [System.Serializable]
    public class ReferenceParams
    {
        // Matches Python JSON: left_ref: [dx0_L, dy0_L]
        public float[] left_ref;
        public float[] right_ref;
    }

    [System.Serializable]
    public class DistanceParams
    {
        public float a;
        public float b;
    }

    [System.Serializable]
    public class CalibratedData
    {
        public ReferenceParams reference;
        public AngleParams angle;
        public DistanceParams distance;
    }

    public static class CalibratorQueueContainer
    {
        public static readonly BlockingCollection<CalibratedData> CalibratorQueue = new BlockingCollection<CalibratedData>();
    }

    [System.Serializable]
    public class EyeVector
    {
        public float dx;
        public float dy;
    }

    [System.Serializable]
    public class EyeVectors
    {
        public EyeVector left_eye_vector;
        public EyeVector right_eye_vector;
    }

    public static class EyeVectorsQueueContainer
    {
        public static readonly BlockingCollection<EyeVectors> EyeVectorsQueue = new BlockingCollection<EyeVectors>();
    }

    public static class TargetDistanceQueueContainer
    {
        public static readonly ConcurrentQueue<float> TargetDistanceQueue = new ConcurrentQueue<float>();
    }
}