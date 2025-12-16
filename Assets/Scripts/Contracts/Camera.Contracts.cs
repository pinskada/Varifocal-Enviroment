using UnityEngine;

namespace Contracts
{
    // Contract for applying orientation updates to the camera.
    public interface ICameraAligner
    {
        // Apply a world-space rotation quaternion.
        void ApplyOrientation(Quaternion worldRotation);

        Quaternion GetCurrentOrientation();

        void NextTarget();
        void PreviousTarget();
    }
}