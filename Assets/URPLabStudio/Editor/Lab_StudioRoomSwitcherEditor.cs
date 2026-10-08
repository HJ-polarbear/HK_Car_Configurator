using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace URPLabStudio.Editor
{
    [CustomEditor(typeof(Lab_StudioRoomSwitcher))]
    internal sealed class Lab_StudioRoomSwitcherEditor : UnityEditor.Editor
    {
        private ReorderableList roomList;

        private void OnEnable()
        {
            roomList = new ReorderableList(serializedObject, serializedObject.FindProperty("rooms"), true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Studio Room Prefabs"),
                elementHeight = EditorGUIUtility.singleLineHeight * 3.5f,
                drawElementCallback = DrawRoom
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AutoRegisterDirectChildPrefabs();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previousButton"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nextButton"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialRoomIndex"));
            EditorGUILayout.Space();
            roomList.DoLayoutList();
            EditorGUILayout.HelpBox("Register the Lab_Studio_A, Lab_Studio_B, and Lab_Studio_C prefab instances under Lab_Studio_Selection. Studio buttons only activate the selected room prefab; the persistent vehicle and all UI remain untouched.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }

        private void AutoRegisterDirectChildPrefabs()
        {
            Lab_StudioRoomSwitcher switcher = (Lab_StudioRoomSwitcher)target;
            SerializedProperty rooms = serializedObject.FindProperty("rooms");
            for (int index = 0; index < rooms.arraySize; index++)
            {
                SerializedProperty slot = rooms.GetArrayElementAtIndex(index);
                SerializedProperty roomPrefab = slot.FindPropertyRelative("roomPrefab");
                if (roomPrefab.objectReferenceValue != null)
                    continue;

                string displayName = slot.FindPropertyRelative("displayName").stringValue;
                string expectedName = displayName.Replace(" ", "_");
                Transform child = switcher.transform.Find(expectedName) ?? switcher.transform.Find("Lab_" + expectedName);
                if (child != null)
                    roomPrefab.objectReferenceValue = child.gameObject;
            }
        }

        private void DrawRoom(Rect rect, int index, bool active, bool focused)
        {
            SerializedProperty slot = serializedObject.FindProperty("rooms").GetArrayElementAtIndex(index);
            float line = EditorGUIUtility.singleLineHeight;
            rect.y += 2f;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), slot.FindPropertyRelative("displayName"));
            rect.y += line + 2f;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), slot.FindPropertyRelative("roomPrefab"), new GUIContent("Room Prefab"));
            rect.y += line + 2f;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, line), slot.FindPropertyRelative("lightingData"), new GUIContent("Lighting Data"));
        }
    }
}
