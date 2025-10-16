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

    public static class GUIQueueContainer
    {
        public static readonly ConcurrentQueue<(Texture2D rawData, int width, int height, EyeSide eyeSide)> eyePreviewQueue =
        new ConcurrentQueue<(Texture2D, int, int, EyeSide)>();
    }
}