using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Contracts
{
    // Contract for applying orientation updates to the camera.
    public interface ISettingsApllier
    {
        // Apply a world-space rotation quaternion.
        void ApplySettings(string settingName, JToken value);

        //void ChangeSettings(string settingName, JToken value);
    }
}