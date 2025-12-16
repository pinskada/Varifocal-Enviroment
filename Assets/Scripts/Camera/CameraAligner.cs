using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Contracts;

public class CameraAligner : MonoBehaviour, ICameraAligner
{
    private List<CameraTarget> targets = new();
    private int currentIndex = 0;
    private bool isReady = false;
    private bool setCameraTargetRotation = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        CalibEvents.CalibStarted += OnCalibStarted;
        CalibEvents.CalibStopped += OnCalibStopped;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        CalibEvents.CalibStarted -= OnCalibStarted;
        CalibEvents.CalibStopped -= OnCalibStopped;
    }

    void OnCalibStarted() => setCameraTargetRotation = true;
    void OnCalibStopped() => setCameraTargetRotation = false;

    private void LateUpdate()
    {
        if (setCameraTargetRotation)
        {
            var rotation = targets[currentIndex].transform.rotation;
            ApplyOrientation(rotation);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // Old targets are about to be destroyed -> drop references immediately
        targets.Clear();
        currentIndex = 0;
        isReady = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitTargetsNextFrame());
    }

    private IEnumerator InitTargetsNextFrame()
    {
        isReady = false;
        yield return null;

        RefreshTargets();

        if (targets.Count == 0)
        {
            Debug.LogWarning("[CameraAligner] No CameraTargets found.");
            yield break;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, targets.Count - 1);
        AlignToCurrent();
        isReady = true;
    }

    private void RefreshTargets() // NEW
    {
        targets = FindObjectsByType<CameraTarget>(FindObjectsSortMode.None)
            .Where(t => t != null)                 // handles destroyed refs
            .OrderBy(t => t.order)
            .ToList();

        if (targets.Count == 0)
            currentIndex = 0;
        else
            currentIndex = Mathf.Clamp(currentIndex, 0, targets.Count - 1);
    }

    private bool EnsureValidCurrent() // NEW
    {
        // Remove destroyed entries (Unity "fake null" safety)
        targets.RemoveAll(t => t == null);

        if (targets.Count == 0) return false;

        if (currentIndex < 0 || currentIndex >= targets.Count)
            currentIndex = Mathf.Clamp(currentIndex, 0, targets.Count - 1);

        // current might still be null if it was destroyed this frame
        if (targets[currentIndex] == null)
            return false;

        return true;
    }

    private void AlignToCurrent()
    {
        if (!EnsureValidCurrent()) return;

        Transform t = targets[currentIndex].transform;
        transform.SetPositionAndRotation(t.position, t.rotation);
    }

    public void NextTarget()
    {
        // In case user presses keys mid-switch, try to recover gracefully
        if (targets.Count == 0) RefreshTargets();
        if (!EnsureValidCurrent()) return;

        currentIndex = (currentIndex + 1) % targets.Count;
        AlignToCurrent();
    }

    public void PreviousTarget()
    {
        if (targets.Count == 0) RefreshTargets();
        if (!EnsureValidCurrent()) return;

        currentIndex--;
        if (currentIndex < 0) currentIndex = targets.Count - 1;
        AlignToCurrent();
    }

    public void ApplyOrientation(Quaternion worldRotation) => transform.rotation = worldRotation;
    public Quaternion GetCurrentOrientation() => transform.rotation;

    public bool IsReady() => isReady; // optional helper
}
