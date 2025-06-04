using UnityEngine;

// Manages the positioning of left and right cameras in the game based on a configurable IPD value.

public class CameraGameIPD : MonoBehaviour
{
    public Transform leftEye; // Reference to the left eye camera transform.
    public Transform rightEye; // Reference to the right eye camera transform.

    private float ipd; // Interpupillary Distance in meters.

    void SetCameraPositions()
    {
        // Sets the positions of the left and right eye cameras based on the Interpupillary Distance (IPD).

        if (leftEye == null || rightEye == null)
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
        leftEye.localPosition = new Vector3(-halfIPD, 0f, 0f);
        rightEye.localPosition = new Vector3(halfIPD, 0f, 0f);
    }

    public void setIPD(float ipd)
    {
        // Set the Interpupillary Distance (IPD) for camera positioning.

        this.ipd = ipd;
        SetCameraPositions();
    }
}
