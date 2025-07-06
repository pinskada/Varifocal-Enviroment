using UnityEngine;

// This script manages the startup settings of the application

public class StartupManager : MonoBehaviour
{
    private void Start()
    {
        // Set the target frame rate to the default value (-1) to allow Unity to manage it automatically.
        Application.targetFrameRate = -1;

        // Set the initial screen resolution and full-screen mode.
        if (Display.displays.Length > 1)
        {
            // Activate second display (external)
            Display.displays[1].Activate();
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(2560, 1440, true);
        }
        else
        {
            Debug.LogWarning("Only one display found – GUI fallback to Display 1.");
        }
    }
}
