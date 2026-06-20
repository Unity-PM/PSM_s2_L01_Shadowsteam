using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(BackgroundAudioManager))]
public class BackgroundAudioManagerEditor : Editor
{
    private readonly BoxBoundsHandle boxHandle = new BoxBoundsHandle();
    private readonly SphereBoundsHandle sphereHandle = new SphereBoundsHandle();

    private void OnSceneGUI()
    {
        BackgroundAudioManager manager = (BackgroundAudioManager)target;
        var zones = manager.EditorMusicZones;
        if (zones == null)
            return;

        for (int i = 0; i < zones.Count; i++)
        {
            BackgroundAudioManager.MusicZone zone = zones[i];
            if (zone == null)
                continue;

            Handles.color = zone.gizmoColor;
            string label = string.IsNullOrEmpty(zone.label) ? $"Music Zone {i}" : zone.label;

            if (zone.shape == BackgroundAudioManager.ZoneShape.Sphere)
                DrawSphereHandle(manager, zone, label);
            else
                DrawBoxHandle(manager, zone, label);
        }
    }

    private void DrawBoxHandle(BackgroundAudioManager manager, BackgroundAudioManager.MusicZone zone, string label)
    {
        EditorGUI.BeginChangeCheck();

        // Position handle moves the whole territory; the bounds handle resizes the faces.
        Vector3 newCenter = Handles.PositionHandle(zone.center, Quaternion.identity);

        boxHandle.center = newCenter;
        boxHandle.size = zone.size;
        boxHandle.wireframeColor = zone.gizmoColor;
        boxHandle.DrawHandle();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(manager, "Edit Music Zone");
            zone.center = boxHandle.center;
            zone.size = boxHandle.size;
            EditorUtility.SetDirty(manager);
        }

        Handles.Label(zone.center + Vector3.up * (Mathf.Abs(zone.size.y) * 0.5f + 0.5f), label);
    }

    private void DrawSphereHandle(BackgroundAudioManager manager, BackgroundAudioManager.MusicZone zone, string label)
    {
        EditorGUI.BeginChangeCheck();

        Vector3 newCenter = Handles.PositionHandle(zone.center, Quaternion.identity);

        sphereHandle.center = newCenter;
        sphereHandle.radius = zone.radius;
        sphereHandle.wireframeColor = zone.gizmoColor;
        sphereHandle.DrawHandle();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(manager, "Edit Music Zone");
            zone.center = sphereHandle.center;
            zone.radius = sphereHandle.radius;
            EditorUtility.SetDirty(manager);
        }

        Handles.Label(zone.center + Vector3.up * (zone.radius + 0.5f), label);
    }
}
