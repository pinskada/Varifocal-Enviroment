using UnityEngine;

[DefaultExecutionOrder(-5)] // run very early
public class UISection : MonoBehaviour
{
    [SerializeField] public string moduleName;

    // Call this from a pre-pass to push moduleName to child fields
    public void ApplyToChildren()
    {
        // if (string.IsNullOrWhiteSpace(moduleName))
        // {
        //     Debug.LogError($"[UISection] No moduleName set for {gameObject.name}.");
        //     return;
        // }
        //Debug.Log($"[UISection] Applying moduleName '{moduleName}' to children of {gameObject.name}");
        foreach (var f in GetComponentsInChildren<UIField>(true)) // true = includeInactive
        {
            f.moduleName = moduleName;
        }
    }

    // Still do it in Awake for safety (works for inactive too), but the pre-pass is decisive
    //private void Awake() => ApplyToChildren();

    // #if UNITY_EDITOR
    //     private void OnValidate() => ApplyToChildren(); // keeps it correct in the editor
    // #endif
}
