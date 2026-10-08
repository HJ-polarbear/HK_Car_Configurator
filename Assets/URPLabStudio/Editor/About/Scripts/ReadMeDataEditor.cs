using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReadMeData))]
public class ReadMeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ReadMeData data = (ReadMeData)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ReadMe Data", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit the About window content from this asset.", MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open About Window"))
            {
                ReadMe.ShowWindow();
            }

            if (GUILayout.Button("Ping Markdown File") && data.markdownFile != null)
            {
                EditorGUIUtility.PingObject(data.markdownFile);
                Selection.activeObject = data.markdownFile;
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }
}