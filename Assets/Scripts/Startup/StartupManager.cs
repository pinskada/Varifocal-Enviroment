using UnityEngine;

// This script manages the startup settings of the application

public class StartupManager : MonoBehaviour
{
    private void Start()
    {
        // Set the target frame rate to the default value (-1) to allow Unity to manage it automatically.
        Application.targetFrameRate = -1;

        // Apply more startup settings here as needed.
        // ...
    }
}
