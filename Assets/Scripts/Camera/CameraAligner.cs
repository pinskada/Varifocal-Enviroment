using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Contracts;

// Position and align the camerarig to a specific anchor GameObject in the scene.

public class CameraAligner : MonoBehaviour, ICameraAligner
{
    [SerializeField] private string anchorName = "CameraTarget"; // Name of the anchor GameObject to align the camera to.


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AlignToAnchorNextFrame());
    }


    private IEnumerator AlignToAnchorNextFrame()
    {
        // Alligns the camera to the specified anchor GameObject after the scene has loaded.

        yield return null; // Wait one frame for objects in the new scene to initialize

        GameObject anchor = GameObject.Find(anchorName);
        if (anchor != null)
        {
            transform.position = anchor.transform.position;
            transform.rotation = anchor.transform.rotation;
            Debug.Log($"Camera aligned to anchor '{anchorName}' in scene.");
        }
        else
        {
            Debug.LogWarning($"No anchor '{anchorName}' found in scene.");
        }
    }


    public void ApplyOrientation(Quaternion worldRotation)
    {
        transform.rotation = worldRotation;
    }


    public Quaternion GetCurrentOrientation()
    {
        return transform.rotation;
    }
}
