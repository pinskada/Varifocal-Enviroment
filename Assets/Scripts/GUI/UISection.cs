using UnityEngine;

public class UISection : MonoBehaviour
{
    public string moduleName;

    private void Awake()
    {
        foreach (var f in GetComponentsInChildren<UIField>(true))
        {
            if (string.IsNullOrEmpty(f.moduleName))
                f.moduleName = moduleName;
        }
    }
}
