using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// This script manages the VR scene transitions and maintains the state of the current scene

public class VRSceneManager : MonoBehaviour
{
    public static VRSceneManager Instance;
    private string currentVRScene;
    public List<string> availableScenes = new() { "SampleScene" };
    private int currentSceneIndex = 0;
    private string previousSceneBeforeCalibration = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Set game objects in the core scene to not be destroyed when loading new scenes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadInitialScenes();
    }

    private IEnumerator LoadInitialScenes()
    {
        // Loads the initial scenes in the correct order

        // Load GUI
        yield return null;
        SceneManager.LoadScene("UI_EditorScene", LoadSceneMode.Additive);

        // Load initial VR scene (SampleScene)
        yield return null;
        SwitchVRScene("SampleScene");
    }

    private IEnumerator SwitchVRScene(string newScene)
    {
        // Check if the new scene is already loaded and skip loading if it is

        bool skipSceneLoad = false;
        if (newScene == currentVRScene)
        {
            Debug.Log($"Scene '{newScene}' already active. Skipping load.");
            skipSceneLoad = true;
        }

        if (!skipSceneLoad)
        {
            if (!string.IsNullOrEmpty(currentVRScene))
                yield return SceneManager.UnloadSceneAsync(currentVRScene);

            yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
            currentVRScene = newScene;
        }
        else
        {
            yield return null; // Wait a frame to ensure the coroutine completes
        }
    }

    public void GoToCalibration()
    {
        // Switches to the calibration scene and stores the previous scene

        previousSceneBeforeCalibration = availableScenes[currentSceneIndex];
        SwitchVRScene("CalibScene");
    }

    public void NextScene()
    {
        // Loads the next scene in the list, or returns to the previous 
        // scene before calibration if currently in the calibration scene

        if (currentVRScene == "CalibScene")
        {
            SwitchVRScene(previousSceneBeforeCalibration ?? availableScenes[0]);
            return;
        }

        currentSceneIndex = (currentSceneIndex + 1) % availableScenes.Count;
        SwitchVRScene(availableScenes[currentSceneIndex]);
    }

    public void PreviousScene()
    {
        // Loads the previous scene in the list, or returns to the previous 
        // scene before calibration if currently in the calibration scene

        if (currentVRScene == "CalibScene")
        {
            SwitchVRScene(previousSceneBeforeCalibration ?? availableScenes[0]);
            return;
        }

        currentSceneIndex = (currentSceneIndex - 1 + availableScenes.Count) % availableScenes.Count;
        SwitchVRScene(availableScenes[currentSceneIndex]);
    }
}
