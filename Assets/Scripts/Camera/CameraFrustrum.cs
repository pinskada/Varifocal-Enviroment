using UnityEngine;

// Applies an asymmetric projection matrix to a stereo camera to account for physical display size.

[RequireComponent(typeof(Camera))]
public class CameraFrustrum : MonoBehaviour
{
    [SerializeField] private bool isLeftEye = true; // Determines if this camera is for the left eye (true) or right eye (false).
    private float screenWidth; // Width of the screen in meters.
    private float screenHeight; // Height of the screen in meters.
    private float eyeToScreenDistance; // Distance from the eye to the screen in meters.
    private float ipd; // Interpupillary distance in meters.
    private Camera eyeCamera; // Reference to the Camera component.

    private float near; // Near clipping plane distance.
    private float far; // Far clipping plane distance.
    private float left; // Left boundary of the frustum.
    private float right; // Right boundary of the frustum.
    private float bottom; // Bottom boundary of the frustum.
    private float top; // Top boundary of the frustum.

    Matrix4x4 proj; // Projection matrix for the camera.

    void Start()
    {
        eyeCamera = GetComponent<Camera>();
        eyeCamera.stereoTargetEye = StereoTargetEyeMask.None;
    }

    void CreateFrustum()
    {
        // Calculates and applies the custom projection matrix for this eye camera.

        if (eyeCamera == null)
        {
            // Return early if the Camera component is not found.
            Debug.LogError("CameraFrustrum: Camera component not found.");
            return;
        }

        if (screenWidth <= 0 || screenHeight <= 0 || eyeToScreenDistance <= 0 || ipd <= 0)
        {
            // Validate parameters to ensure they are set to positive values.
            Debug.LogError("CameraFrustrum: Invalid parameters. Ensure all dimensions are initialized and set to positive values.");
            return;
        }
        
        near = eyeCamera.nearClipPlane;
        far = eyeCamera.farClipPlane;

        // Convert screen space to projection space
        float halfWidth = screenWidth / 2f;
        float halfIPD = (ipd / 1000f) / 2f;
        float eyeOffset = isLeftEye ? -halfIPD : halfIPD;

        // Calculate shift in view frustum due to eye offset
        left = ((-halfWidth - eyeOffset) * near) / eyeToScreenDistance;
        right = ((halfWidth - eyeOffset) * near) / eyeToScreenDistance;
        bottom = (-screenHeight / 2f) * near / eyeToScreenDistance;
        top = (screenHeight / 2f) * near / eyeToScreenDistance;

        // Create the asymmetric projection matrix
        proj = Matrix4x4.Frustum(left, right, bottom, top, near, far);
        eyeCamera.projectionMatrix = proj;
    }

    public void setEyeToScreenDistance(float distance)
    {
        // Sets the distance from the eye to the screen and updates the frustum.

        eyeToScreenDistance = distance;
        CreateFrustum();
    }

    public void setScreenWidth(float width)
    {
        // Sets the screen width and updates the frustum.

        screenWidth = width;
        CreateFrustum();
    }

    public void setScreenHeight(float height)
    {
        // Sets the screen height and updates the frustum.

        screenHeight = height;
        CreateFrustum();
    }

    public void setIPD(float ipd)
    {
        // Sets the interpupillary distance and updates the frustum.

        this.ipd = ipd;
        CreateFrustum();
    }
}
