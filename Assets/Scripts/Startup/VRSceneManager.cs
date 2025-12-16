using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Contracts;

public class VRSceneManager : MonoBehaviour, ISceneManagement
{
    public static VRSceneManager Instance;

    [SerializeField] private string uiSceneName = "UI_EditorScene";
    [SerializeField] private string initialVRSceneName = "CoreScene";

    public List<string> availableScenes = new();

    private string currentVRScene;
    private int currentSceneIndex = 0;

    private bool isSwitching = false;
    private string pendingScene = null;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadInitialScenes());
    }

    private IEnumerator LoadInitialScenes()
    {
        // Load UI additively once
        if (!SceneManager.GetSceneByName(uiSceneName).isLoaded)
            yield return SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);

        // Ensure initial VR scene is loaded (your original code only set the string)
        if (!SceneManager.GetSceneByName(initialVRSceneName).isLoaded)
            yield return SceneManager.LoadSceneAsync(initialVRSceneName, LoadSceneMode.Additive);

        currentVRScene = initialVRSceneName;
    }

    private void RequestSwitch(string newScene)
    {
        if (string.IsNullOrEmpty(newScene))
            return;

        // If a switch is already running, remember only the latest request
        if (isSwitching)
        {
            pendingScene = newScene;
            return;
        }

        StartCoroutine(SwitchVRSceneRoutine(newScene));
    }

    private IEnumerator SwitchVRSceneRoutine(string newScene)
    {
        isSwitching = true;

        // If same scene, do nothing
        if (newScene == currentVRScene)
        {
            isSwitching = false;
            yield break;
        }

        // Unload current (only if it is actually loaded)
        if (!string.IsNullOrEmpty(currentVRScene))
        {
            var cur = SceneManager.GetSceneByName(currentVRScene);
            if (cur.isLoaded)
                yield return SceneManager.UnloadSceneAsync(currentVRScene);
        }

        // Load target (only if not loaded yet)
        var target = SceneManager.GetSceneByName(newScene);
        if (!target.isLoaded)
            yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);

        currentVRScene = newScene;

        isSwitching = false;

        // If another request came in while switching, process it now
        if (!string.IsNullOrEmpty(pendingScene) && pendingScene != currentVRScene)
        {
            var next = pendingScene;
            pendingScene = null;
            RequestSwitch(next);
        }
        else
        {
            pendingScene = null;
        }
    }

    // --- Your public API ---
    public void LoadCalibScene()
    {
        RequestSwitch("CalibScene");
        RenderSettings.ambientIntensity = 0.15f;
    }

    public void NextScene()
    {
        if (availableScenes == null || availableScenes.Count == 0) return;

        if (currentVRScene == "CalibScene")
        {
            RequestSwitch(availableScenes[0]);
            return;
        }

        currentSceneIndex = (currentSceneIndex + 1) % availableScenes.Count;
        RequestSwitch(availableScenes[currentSceneIndex]);
    }

    public void PreviousScene()
    {
        if (availableScenes == null || availableScenes.Count == 0) return;

        if (currentVRScene == "CalibScene")
        {
            RequestSwitch(availableScenes[0]);
            return;
        }

        currentSceneIndex = (currentSceneIndex - 1 + availableScenes.Count) % availableScenes.Count;
        RequestSwitch(availableScenes[currentSceneIndex]);
    }
}
