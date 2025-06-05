using UnityEngine;

// This script manages the startup settings of the application

public class StartupManager : MonoBehaviour
{
    private void Start()
    {
        Application.targetFrameRate = 50;
        QualitySettings.vSyncCount = 1;

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
