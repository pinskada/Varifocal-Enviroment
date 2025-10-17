using UnityEngine;
using Contracts;

// Applies an asymmetric projection matrix to a stereo camera to account for physical display size.

[RequireComponent(typeof(Camera))]
public class CameraFrustrum : MonoBehaviour, IModuleSettingsHandler
{
    [SerializeField] private EyeSide eyeSide; // Determines if this camera is for the left eye (true) or right eye (false).
    private Camera cameraComponent; // Reference to the Camera component.


    void Start()
    {
        GetComponents();
        CheckDisplayParam();
        CheckCameraParam();

        //cameraComponent.stereoTargetEye = StereoTargetEyeMask.None;

        SetCameraPositions();
        CreateFrustrum();
    }


    private void SetCameraPositions()
    {
        // Sets the positions of the left and right eye cameras based on the Interpupillary Distance (IPD).


        // Calculate the half Interpupillary Distance (IPD) for camera positioning.
        float halfIPD = Settings.Display.ipd / 2f;

        Vector3 relativeCamPosition;

        // Offset each camera on the X-axis by half the IPD
        if (eyeSide == EyeSide.Left)
        {
            relativeCamPosition = new Vector3(-halfIPD, 0f, 0f);
        }
        else
        {
            relativeCamPosition = new Vector3(halfIPD, 0f, 0f);
        }

        transform.localPosition = relativeCamPosition;
    }


    private void CreateFrustrum()
    {
        // Calculates and applies the custom projection matrix for this eye camera.


        // Reassing parameters for easier use
        var near = Settings.Display.nearClipPlane;
        var far = Settings.Display.farClipPlane;
        var eyeToScreen = Settings.Display.eyeToScreenDist;
        var height = Settings.Display.screenHeight;
        var width = Settings.Display.screenWidth;
        var ipd = Settings.Display.ipd;

        // Set near and far clip plane
        cameraComponent.nearClipPlane = near;
        cameraComponent.farClipPlane = far;

        // Convert screen space to projection space
        float halfWidth = width / 2f;
        float halfIPD = (ipd / 1000f) / 2f;
        float eyeOffset;

        if (eyeSide == EyeSide.Left)
            eyeOffset = -halfIPD;
        else
            eyeOffset = halfIPD;


        // Calculate shift in view frustum due to eye offset
        var left = ((-halfWidth - eyeOffset) * near) / eyeToScreen;
        var right = ((halfWidth - eyeOffset) * near) / eyeToScreen;
        var bottom = (-height / 2f) * near / eyeToScreen;
        var top = (height / 2f) * near / eyeToScreen;

        // Create the asymmetric projection matrix
        var proj = Matrix4x4.Frustum(left, right, bottom, top, near, far);
        cameraComponent.projectionMatrix = proj;
    }


    private void GetComponents()
    {
        if (eyeSide == EyeSide.None)
        {
            Debug.LogError($"[CameraFrustrum] Eyeside not assigned.");
            return;
        }

        if (GetComponent<Camera>() == null)
        {
            Debug.LogError($"[CameraFrustrum] Camera component for {eyeSide} camera not assigned.");
            return;
        }
        else
            cameraComponent = GetComponent<Camera>();
    }


    private void CheckDisplayParam()
    {
        if (
            Settings.Display.screenWidth <= 0 ||
            Settings.Display.screenHeight <= 0 ||
            Settings.Display.eyeToScreenDist <= 0 ||
            Settings.Display.ipd <= 0
        )
        {
            // Validate parameters to ensure they are set to positive values.
            Debug.LogError("[CameraFrustrum] Invalid parameters. Ensure all dimensions are initialized and set to positive values.");
        }
    }


    private void CheckCameraParam()
    {
        if (Settings.Display.ipd <= 0 || Settings.Display.ipd > 120)
        {
            Debug.LogError($"[CameraFrustrum] Invalid IPD: {Settings.Display.ipd}");
        }

        if (Settings.Display.nearClipPlane <= 0)
        {
            // Validate the near clipping plane to ensure it is set to a positive value.
            Debug.LogError("[CameraFrustrum] Near clip plane must be set to a positive value.");
        }

        if (Settings.Display.nearClipPlane >= Settings.Display.farClipPlane)
        {
            // Ensure the near clip plane is less than the far clip plane.
            Debug.LogError("[CameraFrustrum] Near clip plane must be less than the far clip plane.");
        }
    }


    public void SettingsChanged(string moduleName, string fieldName)
    {
        CheckDisplayParam();
        CheckCameraParam();

        if (fieldName == "nearClipPlane")
            cameraComponent.farClipPlane = Settings.Display.nearClipPlane;

        if (fieldName == "farClipPlane")
            cameraComponent.farClipPlane = Settings.Display.farClipPlane;

        CreateFrustrum();
    }
}
