using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;


namespace Contracts
{
    public enum EyeSide
    {
        None,
        Left,
        Right,
    }

    public interface IGUIHub
    {
        // Sends a list of configuration file names to the GUI for display
        public void pushConfigList(List<string> configFileNames);
    }

    public interface ImageDestroyer
    {
        public void ControlTextures(bool displayTextures);
    }

    public class GUIQueueContainer
    {
        public static readonly ConcurrentQueue<List<EyeImage>> images =
        new ConcurrentQueue<List<EyeImage>>();
        public static readonly ConcurrentQueue<TrackerData> trackerData =
        new ConcurrentQueue<TrackerData>();
    }

    [System.Serializable]
    public class TrackerData
    {
        public EyeData left_eye;
        public EyeData right_eye;
    }

    [System.Serializable]
    public class EyeData
    {
        public float center_x;
        public float center_y;
        public float radius_x;
        public float radius_y;
        public bool is_valid;
    }
}