using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneAudioManager))]
public class SceneAudioManagerEditor : Editor
{
    private SerializedProperty includeInactiveObjectsProp;
    private SerializedProperty audioReferencesProp;

    private void OnEnable()
    {
        includeInactiveObjectsProp = serializedObject.FindProperty("includeInactiveObjects");
        audioReferencesProp = serializedObject.FindProperty("audioReferences");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Editor-only helper: scans the open scene for serialized AudioClip, AudioClip[] and AudioSource fields. It does not play audio at runtime.",
            MessageType.Info
        );

        EditorGUILayout.PropertyField(includeInactiveObjectsProp);

        SceneAudioManager manager = (SceneAudioManager)target;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Audio References"))
        {
            Undo.RecordObject(manager, "Refresh Audio References");
            manager.RefreshAudioReferences();
            EditorUtility.SetDirty(manager);
            serializedObject.Update();
        }

        if (GUILayout.Button("Apply Clip Overrides"))
        {
            Object[] referencedObjects = manager.GetReferencedObjects();
            Undo.RecordObjects(referencedObjects, "Apply Audio Clip Overrides");
            Undo.RecordObject(manager, "Apply Audio Clip Overrides");

            manager.ApplyClipOverrides();

            for (int i = 0; i < referencedObjects.Length; i++)
            {
                if (referencedObjects[i] != null)
                    EditorUtility.SetDirty(referencedObjects[i]);
            }

            EditorUtility.SetDirty(manager);
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"Found References: {audioReferencesProp.arraySize}", EditorStyles.boldLabel);

        for (int i = 0; i < audioReferencesProp.arraySize; i++)
            DrawAudioReference(audioReferencesProp.GetArrayElementAtIndex(i));

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawAudioReference(SerializedProperty entry)
    {
        SerializedProperty ownerProp = entry.FindPropertyRelative("owner");
        SerializedProperty componentTypeProp = entry.FindPropertyRelative("componentType");
        SerializedProperty fieldNameProp = entry.FindPropertyRelative("fieldName");
        SerializedProperty elementIndexProp = entry.FindPropertyRelative("elementIndex");
        SerializedProperty currentClipProp = entry.FindPropertyRelative("currentClip");
        SerializedProperty overrideClipProp = entry.FindPropertyRelative("overrideClip");
        SerializedProperty audioSourceProp = entry.FindPropertyRelative("audioSource");

        string fieldName = fieldNameProp.stringValue;
        if (elementIndexProp.intValue >= 0)
            fieldName += $"[{elementIndexProp.intValue}]";

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"{componentTypeProp.stringValue}.{fieldName}", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(ownerProp);
            if (audioSourceProp.objectReferenceValue != null)
                EditorGUILayout.PropertyField(audioSourceProp);
            EditorGUILayout.PropertyField(currentClipProp);
        }

        EditorGUILayout.PropertyField(overrideClipProp);
        EditorGUILayout.EndVertical();
    }
}
