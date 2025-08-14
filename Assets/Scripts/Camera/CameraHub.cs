using UnityEngine;
using Contracts;

// Manages the camera setup for a VR application, including setting up the left and right eye frustrums and managing IPD.

public class CameraHub : MonoBehaviour, ICameraHub
{
    [SerializeField] private CameraFrustrum leftEyeFrustrum; // Reference to the left eye camera frustrum.
    [SerializeField] private CameraFrustrum rightEyeFrustrum; // Reference to the right eye camera frustrum.
    [SerializeField] private CameraGameProperties cameraGameProperties; // Reference to the camera game IPD manager.

    private float ipd = 0.063f; // Default Interpupillary Distance in meters
    private float FOV = 75f; // Default field of view in degrees
    private float nearClipPlane = 0.01f; // Default near clipping plane in meters
    private float farClipPlane = 100f; // Default far clipping plane in meters
    private float eyeToScreenDistance = 0.06f; // Default distance from eye to screen in meters
    private float screenWidth = 0.12f; // Default screen width in meters
    private float screenHeight = 0.068f; // Default screen height in meters
    public Transform target; // Target transform to apply the camera orientation to

    void Start()
    {
        SetInitialParameters();
    }

    private void SetInitialParameters()
    {
        // Populate the camera frustrums and IPD settings with default values.

        SetIPD(ipd);
        SetFOV(FOV);
        SetNearClipPlane(nearClipPlane);
        SetFarClipPlane(farClipPlane);
        SetEyeToScreenDistance(eyeToScreenDistance);
        SetScreenWidth(screenWidth);
        SetScreenHeight(screenHeight);
    }

    public void ApplyOrientation(Quaternion worldRotation)
    {
        target.rotation = worldRotation;
    }

    public Quaternion GetCurrentOrientation()
    {
        return target.rotation;
    }

    public void SetFOV(float fov)
    {
        // Set the field of view for both cameras.

        this.FOV = fov;
        cameraGameProperties.SetFOV(fov);
    }

    public void SetNearClipPlane(float nearClipPlane)
    {
        // Set the near clipping plane for both cameras.

        this.nearClipPlane = nearClipPlane;
        cameraGameProperties.SetNearClipPlane(nearClipPlane);
    }

    public void SetFarClipPlane(float farClipPlane)
    {
        // Set the far clipping plane for both cameras.

        this.farClipPlane = farClipPlane;
        cameraGameProperties.SetFarClipPlane(farClipPlane);
    }

    public void SetIPD(float ipd)
    {
        // Set the Interpupillary Distance (IPD) for both eye frustrums and the camera game IPD manager.

        this.ipd = ipd;
        cameraGameProperties.SetIPD(ipd);
        leftEyeFrustrum.setIPD(ipd);
        rightEyeFrustrum.setIPD(ipd);
    }

    public void SetEyeToScreenDistance(float distance)
    {
        // Set the distance from the eye to the screen for both eye frustrums.

        eyeToScreenDistance = distance;
        leftEyeFrustrum.setEyeToScreenDistance(distance);
        rightEyeFrustrum.setEyeToScreenDistance(distance);
    }

    public void SetScreenWidth(float width)
    {
        // Set the screen width for both eye frustrums.

        screenWidth = width;
        leftEyeFrustrum.setScreenWidth(width);
        rightEyeFrustrum.setScreenWidth(width);
    }

    public void SetScreenHeight(float height)
    {
        // Set the screen height for both eye frustrums.

        screenHeight = height;
        leftEyeFrustrum.setScreenHeight(height);
        rightEyeFrustrum.setScreenHeight(height);
    }
}
