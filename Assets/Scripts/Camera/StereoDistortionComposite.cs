using UnityEngine;
using System.Collections;
using Contracts;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class StereoDistortionComposite : MonoBehaviour, IModuleSettingsHandler
{
    [SerializeField] private Material distortionMaterialLeft;
    [SerializeField] private Material distortionMaterialRight;

    [SerializeField] private StereoRTManager rtManager;
    private float compositeCameraDepth = 1000f;
    private Camera _cam;
    private int currentDisplay;
    private bool modifyDistortion = false;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        CheckComponentsExistence();

        _cam.cullingMask = 0;
        _cam.clearFlags = CameraClearFlags.Nothing;
        _cam.depth = compositeCameraDepth; // render last
        _cam.targetTexture = null; // render to a display (backbuffer)
    }


    IEnumerator Start()
    {
        // Give Unity a frame to enumerate displays on Windows
        yield return null;

        // Choose where to present: prefer Display 2 if present, else fall back to Display 1
        if (Display.displays.Length >= 2)
        {
            Display.displays[1].Activate();
            currentDisplay = 1;
            _cam.targetDisplay = currentDisplay; // Display 2
            Debug.Log("[StereoDistortionComposite] Using Display 2 for stereo composite output.");
        }
        else
        {
            currentDisplay = 0;
            _cam.targetDisplay = currentDisplay; // Display 1
            Debug.LogWarning("[StereoDistortionComposite] Only one display found.");
        }

        ApplyMaterialSettings();
    }


    void Update()
    {
        if (modifyDistortion == true)
        {
            ApplyMaterialSettings();
            modifyDistortion = false;
        }
    }

    private void CheckComponentsExistence()
    {
        if (rtManager == null)
        {
            Debug.LogError("[StereoDistortionComposite] StereoRTManager is missing!");
        }

        if (distortionMaterialLeft == null)
        {
            Debug.LogError("[StereoDistortionComposite] Left Distortion Material is missing!");
        }

        if (distortionMaterialRight == null)
        {
            Debug.LogError("[StereoDistortionComposite] Right Distortion Material is missing!");
        }
    }


    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Graphics.Blit(src, dst);

        if (rtManager.leftRT == null || rtManager.rightRT == null)
        {
            Debug.LogWarning("[StereoDistortionComposite] Missing references, or first image, cannot composite stereo images.");
            return;
        }

        // Use target display resolution
        int outW = Display.displays[currentDisplay].systemWidth;
        int outH = Display.displays[currentDisplay].systemHeight;

        if (outW <= 0 || outH <= 0)
        {
            Graphics.Blit(src, dst);
            Debug.LogWarning($"[StereoDistortionComposite] Invalid display resolution: " + outW + "x" + outH);
            return;
        }

        bool pushed = false;
        var prevRT = RenderTexture.active;

        try
        {
            // Set render target: null means backbuffer
            Graphics.SetRenderTarget(dst);

            int halfW = outW / 2;

            // Use pixel matrix in output resolution
            GL.PushMatrix();
            pushed = true;
            GL.LoadPixelMatrix(0, outW, outH, 0);

            // Left half
            Graphics.DrawTexture(
                new Rect(0, 0, halfW, outH),
                rtManager.leftRT,
                distortionMaterialLeft
            );

            // Right half
            Graphics.DrawTexture(
                new Rect(halfW, 0, halfW, outH),
                rtManager.rightRT,
                distortionMaterialRight
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[StereoDistortionComposite] OnRenderImage error: {ex.Message}");
        }
        finally
        {
            if (pushed) GL.PopMatrix();        // 3) Always balance the stack
            RenderTexture.active = prevRT;     // restore active RT
        }
    }

    private void ApplyMaterialSettings()
    {

        var halfWidth = Settings.display.screenWidth / 1000f / 2f; // in meters
        var halfIPD = Settings.display.ipd / 1000f / 2f; // in meters

        var xOffset = halfIPD / halfWidth;

        var leftCenter = new Vector2(1 - xOffset, 0.5f);
        var rightCenter = new Vector2(xOffset, 0.5f);

        float overscan = Settings.display.preZoom;
        float clampBlack = Settings.display.clampBlack;
        if (clampBlack < 0.5f) clampBlack = 0f;
        if (clampBlack >= 0.5f) clampBlack = 1f;

        //Debug.Log($"[StereoDistortionComposite] Left center: {leftCenter}, Right center: {rightCenter}");

        distortionMaterialLeft.SetFloat("_Strength", Settings.display.distortionStrength);
        distortionMaterialRight.SetFloat("_Strength", Settings.display.distortionStrength);

        distortionMaterialLeft.SetVector("_Center", leftCenter);
        distortionMaterialRight.SetVector("_Center", rightCenter);

        distortionMaterialLeft.SetFloat("_ClampBlack", clampBlack);
        distortionMaterialRight.SetFloat("_ClampBlack", clampBlack);

        distortionMaterialLeft.SetFloat("_PreScale", overscan);
        distortionMaterialRight.SetFloat("_PreScale", overscan);
    }

    public void SettingsChanged(string moduleName, string fieldName)
    {
        Debug.Log($"[StereoDistortionComposite] Settings changed: {moduleName}.{fieldName}, marking for update.");
        modifyDistortion = true;
    }
}
