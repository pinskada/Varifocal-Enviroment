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
    }
}