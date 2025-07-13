using UnityEngine;
//using Contracts;

namespace Startup {
    [DisallowMultipleComponent]
    public class Bootstrapper : MonoBehaviour {
        [Header("Drag in your core components (persistent across scenes)")]
        public NetworkManager networkManager;        // receives raw IMU JSON and produces IMUData
        public IMUHandler imuHandler;                  // filters IMUData and computes orientation
        public CameraHub cameraHub;                // applies orientation to camera frustrums 
        void Awake()
        {
            // Prevent duplicate bootstrappers when scenes reload
            if (Object.FindObjectsByType<Bootstrapper>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            // 1) Wire NetworkManager → IMUHandler (filter pipeline)
            //    NetworkManager calls imuHandler.UpdateFilter(IMUData)
            networkManager.InitializeDataReceiver(imuHandler);


            // 2) Wire IMUHandler → OrientationApplier (apply filtered orientation)
            //    IMUHandler calls applier.ApplyOrientation(Quaternion)
            imuHandler.InjectOrientationApplier(cameraHub);
        }
    }
}
