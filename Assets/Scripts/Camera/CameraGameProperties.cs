using UnityEngine;

// Manages the positioning of left and right cameras in the game based on a configurable IPD value.

public class CameraGameProperties : MonoBehaviour
{
    [SerializeField] private Transform leftEyeIPD; // Reference to the left eye camera transform.
    [SerializeField] private Transform rightEyeIPD; // Reference to the right eye camera transform.

    [SerializeField] private Camera leftEyeCamera; // Reference to the left eye camera component.
    [SerializeField] private Camera rightEyeCamera; // Reference to the right eye camera component

    private float ipd; // Interpupillary Distance in meters.

    void SetCameraPositions()
    {
        // Sets the positions of the left and right eye cameras based on the Interpupillary Distance (IPD).

        if (leftEyeIPD == null || rightEyeIPD == null)
        {
            // Ensure both left and right eye transforms are assigned.
            Debug.LogError("CameraGameIPD: Left or Right eye transform is not assigned.");
            return;
        }

        if (ipd <= 0)
        {
            // Validate the Interpupillary Distance (IPD) to ensure it is set to a positive value.
            Debug.LogError("CameraGameIPD: IPD not initialized or must be set to a positive value.");
            return;
        }

        // Calculate the half Interpupillary Distance (IPD) for camera positioning.
        float halfIPD = ipd / 2f;

        // Offset each camera on the X-axis by half the IPD
        leftEyeIPD.localPosition = new Vector3(-halfIPD, 0f, 0f);
        rightEyeIPD.localPosition = new Vector3(halfIPD, 0f, 0f);
    }

    public void SetIPD(float ipd)
    {
        // Set the Interpupillary Distance (IPD) for camera positioning.

        this.ipd = ipd;
        SetCameraPositions();
    }

    public void SetFOV(float fov)
    {
        // Set the field of view for both left and right eye cameras.

        if (!CheckCameraExistence())
        {
            return; // Exit if cameras are not properly assigned.
        }

        if (fov <= 0 || fov > 180)
        {
            // Validate the field of view to ensure it is set to a positive value.
            Debug.LogWarning("CameraGameProperties: FOV must be set to a positive value or less than 180°.");
            return;
        }

        leftEyeCamera.fieldOfView = fov;
        rightEyeCamera.fieldOfView = fov;
    }

    public void SetNearClipPlane(float nearClipPlane)
    {
        // Set the near clipping plane for both left and right eye cameras.

        if (!CheckCameraExistence())
        {
            return; // Exit if cameras are not properly assigned.
        }

        if (nearClipPlane <= 0)
        {
            // Validate the near clipping plane to ensure it is set to a positive value.
            Debug.LogWarning("CameraGameProperties: Near clip plane must be set to a positive value.");
            return;
        }

        if (nearClipPlane >= rightEyeCamera.farClipPlane || nearClipPlane >= leftEyeCamera.farClipPlane)
        {
            // Ensure the near clip plane is less than the far clip plane.
            Debug.LogWarning("CameraGameProperties: Near clip plane must be less than the far clip plane.");
            return;
        }

        leftEyeCamera.nearClipPlane = nearClipPlane;
        rightEyeCamera.nearClipPlane = nearClipPlane;
    }

    public void SetFarClipPlane(float farClipPlane)
    {
        // Set the far clipping plane for both left and right eye cameras.

        if (!CheckCameraExistence())
        {
            return; // Exit if cameras are not properly assigned.
        }

        if (farClipPlane <= 0)
        {
            // Validate the far clipping plane to ensure it is set to a positive value.
            Debug.LogWarning("CameraGameProperties: Far clip plane must be set to a positive value.");
            return;
        }

        if (farClipPlane <= rightEyeCamera.nearClipPlane || farClipPlane <= leftEyeCamera.nearClipPlane)
        {
            // Ensure the near clip plane is less than the far clip plane.
            Debug.LogWarning("CameraGameProperties: Far clip plane must be less than the near clip plane.");
            return;
        }

        leftEyeCamera.farClipPlane = farClipPlane;
        rightEyeCamera.farClipPlane = farClipPlane;
    }
    
    private bool CheckCameraExistence()
    {
        // Ensure that both left and right eye cameras are assigned.
        if (leftEyeCamera == null || rightEyeCamera == null)
        {
            Debug.LogError("CameraGameProperties: Left or Right eye camera is not assigned.");
            return false;
        }
        else
        {
            return true;
        }
    }
}
