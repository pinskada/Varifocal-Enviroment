using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class StereoDistortionComposite : MonoBehaviour
{
    [SerializeField] private Material distortionMaterial;

    private StereoRTManager rtManager;
    private float compositeCameraDepth = 1000f;
    private Camera _cam;


    void Awake()
    {
        _cam = GetComponent<Camera>();

        // Composite camera renders nothing from the scene; it only draws the final blit
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
            _cam.targetDisplay = 1; // Display 2
            Debug.Log("[StereoDistortionComposite] Using Display 2 for stereo composite output.");
        }
        else
        {
            Debug.LogError("[StereoDistortionComposite] Only one display found.");
        }
    }


    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (rtManager == null || distortionMaterial == null ||
            rtManager.leftRT == null || rtManager.rightRT == null)
        {
            Debug.LogWarning("[StereoDistortionComposite] Missing references, or first image, cannot composite stereo images.");
            Graphics.Blit(src, dst);
            return;
        }

        // We’ll draw into dst twice: left half and right half.
        Graphics.SetRenderTarget(dst);
        GL.PushMatrix();
        // Pixel space (0..width, 0..height) so we can draw exact halves
        GL.LoadPixelMatrix(0, dst.width, dst.height, 0);

        // Left half
        Graphics.DrawTexture(
            new Rect(0, 0, dst.width / 2, dst.height), // Left half of the dst
            rtManager.leftRT, // Use the left eye RT
            distortionMaterial // Material with distortion shader
        );

        // Right half
        Graphics.DrawTexture(
            new Rect(dst.width / 2, 0, dst.width / 2, dst.height), // Right half of the dst
            rtManager.rightRT, // Use the right eye RT
            distortionMaterial // Material with distortion shader
        );

        GL.PopMatrix();
    }
}
