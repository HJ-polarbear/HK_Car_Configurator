namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement("URPLab/Lab_ResetTransform")]
public class ResetTransformButton : EditorToolbarButton
{
    private const string kIconPath = "Assets/URPLabStudio/Editor/Icons/reset.png";

    public ResetTransformButton()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(kIconPath);
        icon = tex != null
            ? tex
            : (EditorGUIUtility.IconContent("d_Refresh").image as Texture2D);

        tooltip = "Reset Transform of Selected Object(s)";
        clicked += OnClicked;
    }

    private static void OnClicked()
    {
        Transform[] targets = Selection.transforms;
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("No object selected.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        Object[] objs = new Object[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            objs[i] = targets[i];
        }

        Undo.RecordObjects(objs, "Reset Transform");

        foreach (Transform t in targets)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        Undo.CollapseUndoOperations(group);

        Debug.Log($"Reset transform for {targets.Length} object(s).");
    }
}
}
