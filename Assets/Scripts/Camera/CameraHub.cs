using UnityEngine;
using System.Collections;

// Manages the camera setup for a VR application, including setting up the left and right eye frustrums and managing IPD.

public class CameraHub : MonoBehaviour
{
    [SerializeField] private CameraFrustrum leftEyeFrustrum; // Reference to the left eye camera frustrum.
    [SerializeField] private CameraFrustrum rightEyeFrustrum; // Reference to the right eye camera frustrum.
    [SerializeField] private CameraGameIPD cameraGameIPD; // Reference to the camera game IPD manager.

    private float ipd = 0.063f; // Default Interpupillary Distance in meters
    private float eyeToScreenDistance = 0.06f; // Default distance from eye to screen in meters
    private float screenWidth = 0.12f; // Default screen width in meters
    private float screenHeight = 0.068f; // Default screen height in meters


    void Start()
    {
        SetInitialParameters();
    }

    private void SetInitialParameters()
    {
        // Populate the camera frustrums and IPD settings with default values.

        SetIPD(ipd);
        SetEyeToScreenDistance(eyeToScreenDistance);
        SetScreenWidth(screenWidth);
        SetScreenHeight(screenHeight);
    }

    public void SetIPD(float ipd)
    {
        // Set the Interpupillary Distance (IPD) for both eye frustrums and the camera game IPD manager.

        this.ipd = ipd;
        cameraGameIPD.setIPD(ipd);
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
