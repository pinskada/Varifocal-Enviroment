using UnityEngine;
using Contracts;

// Applies an asymmetric projection matrix to a stereo camera to account for physical display size.

[RequireComponent(typeof(Camera))]
public class CameraFrustrum : MonoBehaviour, IModuleSettingsHandler
{
    [SerializeField] private EyeSide eyeSide; // Determines if this camera is for the left eye (true) or right eye (false).
    private Camera cameraComponent; // Reference to the Camera component.
    private bool createFrustrum = false;

    void Start()
    {
        GetComponents();
        CheckDisplayParam();
        CheckCameraParam();

        //cameraComponent.stereoTargetEye = StereoTargetEyeMask.None;

        SetCameraPositions();
        CreateFrustrum();
    }


    void Update()
    {
        if (createFrustrum)
        {
            CreateFrustrum();
            createFrustrum = false;
        }
    }

    private void SetCameraPositions()
    {
        // Sets the positions of the left and right eye cameras based on the Interpupillary Distance (IPD).


        // Calculate the half Interpupillary Distance (IPD) for camera positioning.
        float halfIPD = Settings.display.ipd / 1000f / 2f;

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
        var near = Settings.display.eyeToScreenDist / 1000f; // in meters
        var far = Settings.display.farClipPlane; // in meters

        var height = Settings.display.screenHeight / 1000f; // in millimeters
        var width = Settings.display.screenWidth / 1000f; // in millimeters
        var ipd = Settings.display.ipd / 1000f; // in millimeters

        // Set near and far clip plane
        cameraComponent.nearClipPlane = near;
        cameraComponent.farClipPlane = far;

        // Convert screen space to projection space
        float halfWidth = width / 2f;
        float halfIPD = ipd / 2f;

        float eyeOffset = (eyeSide == EyeSide.Left) ? -halfIPD : halfIPD;


        var left = 0f;
        var right = 0f;

        // Calculate shift in view frustum due to eye offset
        if (eyeSide == EyeSide.Left)
        {
            left = -halfWidth - eyeOffset;
            right = 0 - eyeOffset;
        }
        else
        {
            left = 0 - eyeOffset;
            right = halfWidth - eyeOffset;
        }

        var bottom = -height / 2f;
        var top = height / 2f;

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
            Settings.display.screenWidth <= 0 ||
            Settings.display.screenHeight <= 0 ||
            Settings.display.eyeToScreenDist <= 0 ||
            Settings.display.ipd <= 0
        )
        {
            // Validate parameters to ensure they are set to positive values.
            Debug.LogError("[CameraFrustrum] Invalid parameters. Ensure all dimensions are initialized and set to positive values.");
        }
    }


    private void CheckCameraParam()
    {
        if (Settings.display.ipd <= 0 || Settings.display.ipd > 120)
        {
            Debug.LogError($"[CameraFrustrum] Invalid IPD: {Settings.display.ipd}");
        }

        if (Settings.display.eyeToScreenDist >= Settings.display.farClipPlane)
        {
            // Ensure the near clip plane is less than the far clip plane.
            Debug.LogError("[CameraFrustrum] Near clip plane must be less than the far clip plane.");
        }
    }


    public void SettingsChanged(string moduleName, string fieldName)
    {
        //Debug.Log($"[CameraFrustrum] Settings changed: {moduleName} - {fieldName} - {Settings.display.ipd}");

        CheckDisplayParam();
        CheckCameraParam();

        createFrustrum = true;
    }
}
