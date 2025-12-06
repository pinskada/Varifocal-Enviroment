using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Contracts;
using UnityEngine.Rendering;

// This script manages the VR scene transitions and maintains the state of the current scene

public class VRSceneManager : MonoBehaviour, ISceneManagement
{
    public static VRSceneManager Instance;
    private string currentVRScene = null;
    public List<string> availableScenes = new() { };
    private int currentSceneIndex = 0;

    void Awake()
    {
        // Set game objects in the core scene to not be destroyed when loading new scenes
        DontDestroyOnLoad(gameObject);

        StartCoroutine(LoadInitialScenes());
    }

    private IEnumerator LoadInitialScenes()
    {
        // Loads the initial scenes in the correct order

        // Load GUI
        //yield return null;
        SceneManager.LoadScene("UI_EditorScene", LoadSceneMode.Additive);
        // Load initial VR scene (CoreScene)
        yield return null;
        currentVRScene = "CoreScene";
        //SceneManager.LoadSceneAsync("CoreScene", LoadSceneMode.Additive);

    }

    private IEnumerator SwitchVRScene(string newScene)
    {
        // Checks if the new scene is already loaded and skip loading if it is

        if (string.IsNullOrEmpty(newScene))
        {
            Debug.LogWarning("[VRSceneManager] New scene name is null or empty. Aborting scene switch.");
            yield return null;
        }

        if (newScene == currentVRScene)
        {
            Debug.Log($"[VRSceneManager] Scene '{newScene}' already active. Skipping load.");
            yield return null;
        }

        yield return SceneManager.UnloadSceneAsync(currentVRScene);

        yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);

        currentVRScene = newScene;
    }

    public void LoadCalibScene()
    {
        // Switches to the calibration scene and stores the previous scene

        Debug.Log("Load Calib Scene called");

        StartCoroutine(SwitchVRScene("CalibScene"));
        currentVRScene = "CalibScene";
        RenderSettings.ambientIntensity = 0.15f;
    }

    public void NextScene()
    {
        // Loads the next scene in the list, or returns to the previous
        // scene before calibration if currently in the calibration scene

        Debug.Log("Next Scene called");

        if (currentVRScene == "CalibScene")
        {
            StartCoroutine(SwitchVRScene(availableScenes[0]));
            return;
        }

        currentSceneIndex = (currentSceneIndex + 1) % availableScenes.Count;
        Debug.Log("New scene index: " + currentSceneIndex);

        StartCoroutine(SwitchVRScene(availableScenes[currentSceneIndex]));
    }

    public void PreviousScene()
    {
        // Loads the previous scene in the list, or returns to the previous
        // scene before calibration if currently in the calibration scene


        if (currentVRScene == "CalibScene")
        {
            StartCoroutine(SwitchVRScene(availableScenes[0]));
            return;
        }

        currentSceneIndex = (currentSceneIndex - 1 + availableScenes.Count) % availableScenes.Count;
        Debug.Log("New scene index: " + currentSceneIndex);
        StartCoroutine(SwitchVRScene(availableScenes[currentSceneIndex]));
    }
}
