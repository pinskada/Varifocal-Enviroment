using UnityEngine;

public class StereoRTManager : MonoBehaviour
{
    [Header("Eye cameras")]
    public Camera leftCam;
    public Camera rightCam;

    private int depth = 24; // Depth buffer bits
    private int eyeWidth; // Per-eye resolution width
    private int eyeHeight; // Per-eye resolution height

    [HideInInspector] public RenderTexture leftRT;
    [HideInInspector] public RenderTexture rightRT;


    void Start()
    {
        if (Display.displays.Length > 1)
        {
            eyeHeight = Display.displays[1].systemHeight;
            eyeWidth = Display.displays[1].systemWidth / 2;
        }
        else
        {
            eyeHeight = Display.displays[0].systemHeight;
            eyeWidth = Display.displays[0].systemWidth / 2;
        }

        // Create RenderTextures
        leftRT = new RenderTexture(eyeWidth, eyeHeight, depth, RenderTextureFormat.Default);
        rightRT = new RenderTexture(eyeWidth, eyeHeight, depth, RenderTextureFormat.Default);

        leftRT.Create();
        rightRT.Create();

        // Assign to cameras
        leftCam.targetTexture = leftRT;
        rightCam.targetTexture = rightRT;

        Debug.Log("[StereoRTManager] Left and Right RenderTextures created and assigned.");
    }


    void OnDisable()
    {
        // Cleanup when script is disabled/destroyed
        if (leftRT != null) leftRT.Release();
        if (rightRT != null) rightRT.Release();
    }
}
