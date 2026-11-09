using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// For keyboard testing (XR Device Simulator):
// "Toggle" = press Grip once to grab and once more to let go,
// without holding the key down.
// Switch back to "Hold" before testing with the headset.
public static class SimulatorGrabMode
{
    [MenuItem("Little Bloom/Grab Mode/Toggle (for keyboard testing)")]
    public static void SetToggle()
    {
        SetMode(XRBaseInputInteractor.InputTriggerType.Toggle);
    }

    [MenuItem("Little Bloom/Grab Mode/Hold (normal, for headset)")]
    public static void SetHold()
    {
        SetMode(XRBaseInputInteractor.InputTriggerType.StateChange);
    }

    static void SetMode(XRBaseInputInteractor.InputTriggerType mode)
    {
        NearFarInteractor[] hands = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < hands.Length; i++)
        {
            Undo.RecordObject(hands[i], "Grab Mode");
            hands[i].selectActionTrigger = mode;
            PrefabUtility.RecordPrefabInstancePropertyModifications(hands[i]);
        }

        if (hands.Length > 0) EditorSceneManager.MarkSceneDirty(hands[0].gameObject.scene);
        Debug.Log("Grab mode: " + mode + " (" + hands.Length + " hands). Save the scene (Ctrl+S).");
    }
}
