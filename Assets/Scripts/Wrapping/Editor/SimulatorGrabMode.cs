using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Pentru testat cu tastatura (XR Device Simulator):
// "Toggle" = apesi Grip o data ca sa apuci si inca o data ca sa dai drumul,
// fara sa tii tasta apasata tot drumul.
// Inainte de testul cu headset-ul, pune inapoi "Hold".
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
        Debug.Log("Grab mode: " + mode + " (" + hands.Length + " maini). Salveaza scena (Ctrl+S).");
    }
}
