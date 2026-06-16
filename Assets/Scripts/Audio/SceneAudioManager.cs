using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class SceneAudioManager : MonoBehaviour
{
    [Serializable]
    public class AudioReferenceEntry
    {
        [SerializeField] private MonoBehaviour owner;
        [SerializeField] private string componentType;
        [SerializeField] private string fieldName;
        [SerializeField] private int elementIndex = -1;
        [SerializeField] private AudioClip currentClip;
        [SerializeField] private AudioClip overrideClip;
        [SerializeField] private AudioSource audioSource;

        public MonoBehaviour Owner => owner;
        public string ComponentType => componentType;
        public string FieldName => fieldName;
        public int ElementIndex => elementIndex;
        public AudioClip CurrentClip => currentClip;
        public AudioClip OverrideClip => overrideClip;
        public AudioSource AudioSource => audioSource;

        public AudioReferenceEntry(
            MonoBehaviour owner,
            string componentType,
            string fieldName,
            int elementIndex,
            AudioClip currentClip,
            AudioSource audioSource)
        {
            this.owner = owner;
            this.componentType = componentType;
            this.fieldName = fieldName;
            this.elementIndex = elementIndex;
            this.currentClip = currentClip;
            this.audioSource = audioSource;
        }
    }

    private const BindingFlags AudioFieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Header("Editor Scan")]
    [SerializeField] private bool includeInactiveObjects = true;
    [SerializeField] private List<AudioReferenceEntry> audioReferences = new List<AudioReferenceEntry>();

    public IReadOnlyList<AudioReferenceEntry> AudioReferences => audioReferences;

    [ContextMenu("Refresh Audio References")]
    public void RefreshAudioReferences()
    {
        audioReferences.Clear();

        FindObjectsInactive inactiveMode = includeInactiveObjects
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            inactiveMode,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour owner = behaviours[i];
            if (owner == null || owner == this)
                continue;

            CollectAudioFields(owner);
        }
    }

    [ContextMenu("Apply Clip Overrides")]
    public void ApplyClipOverrides()
    {
        for (int i = 0; i < audioReferences.Count; i++)
        {
            AudioReferenceEntry entry = audioReferences[i];
            if (entry == null || entry.Owner == null || entry.OverrideClip == null)
                continue;

            TryApplyClipOverride(entry);
        }

        RefreshAudioReferences();
    }

    public UnityEngine.Object[] GetReferencedObjects()
    {
        List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        for (int i = 0; i < audioReferences.Count; i++)
        {
            AudioReferenceEntry entry = audioReferences[i];
            if (entry == null)
                continue;

            if (entry.Owner != null && !objects.Contains(entry.Owner))
                objects.Add(entry.Owner);

            if (entry.AudioSource != null && !objects.Contains(entry.AudioSource))
                objects.Add(entry.AudioSource);
        }

        return objects.ToArray();
    }

    private void CollectAudioFields(MonoBehaviour owner)
    {
        Type type = owner.GetType();
        string componentType = type.Name;

        for (Type current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
        {
            FieldInfo[] fields = current.GetFields(AudioFieldFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (!ShouldInspectField(field))
                    continue;

                CollectField(owner, componentType, field);
            }
        }
    }

    private void CollectField(MonoBehaviour owner, string componentType, FieldInfo field)
    {
        Type fieldType = field.FieldType;
        object value = field.GetValue(owner);

        if (typeof(AudioClip).IsAssignableFrom(fieldType))
        {
            audioReferences.Add(new AudioReferenceEntry(
                owner,
                componentType,
                field.Name,
                -1,
                value as AudioClip,
                null
            ));
            return;
        }

        if (typeof(AudioSource).IsAssignableFrom(fieldType))
        {
            AudioSource source = value as AudioSource;
            audioReferences.Add(new AudioReferenceEntry(
                owner,
                componentType,
                field.Name,
                -1,
                source != null ? source.clip : null,
                source
            ));
            return;
        }

        if (fieldType.IsArray && fieldType.GetElementType() == typeof(AudioClip))
        {
            AudioClip[] clips = value as AudioClip[];
            if (clips == null)
                return;

            for (int i = 0; i < clips.Length; i++)
                audioReferences.Add(new AudioReferenceEntry(owner, componentType, field.Name, i, clips[i], null));

            return;
        }

        if (fieldType.IsArray && fieldType.GetElementType() == typeof(AudioSource))
        {
            AudioSource[] sources = value as AudioSource[];
            if (sources == null)
                return;

            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                audioReferences.Add(new AudioReferenceEntry(
                    owner,
                    componentType,
                    field.Name,
                    i,
                    source != null ? source.clip : null,
                    source
                ));
            }
        }
    }

    private static bool ShouldInspectField(FieldInfo field)
    {
        if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized)
            return false;

        bool serialized = field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField));
        if (!serialized)
            return false;

        Type fieldType = field.FieldType;
        return typeof(AudioClip).IsAssignableFrom(fieldType)
            || typeof(AudioSource).IsAssignableFrom(fieldType)
            || (fieldType.IsArray && fieldType.GetElementType() == typeof(AudioClip))
            || (fieldType.IsArray && fieldType.GetElementType() == typeof(AudioSource));
    }

    private static bool TryApplyClipOverride(AudioReferenceEntry entry)
    {
        FieldInfo field = FindField(entry.Owner.GetType(), entry.FieldName);
        if (field == null)
            return false;

        object value = field.GetValue(entry.Owner);
        if (typeof(AudioClip).IsAssignableFrom(field.FieldType))
        {
            field.SetValue(entry.Owner, entry.OverrideClip);
            return true;
        }

        if (field.FieldType.IsArray && field.FieldType.GetElementType() == typeof(AudioClip))
        {
            AudioClip[] clips = value as AudioClip[];
            if (clips == null || entry.ElementIndex < 0 || entry.ElementIndex >= clips.Length)
                return false;

            clips[entry.ElementIndex] = entry.OverrideClip;
            field.SetValue(entry.Owner, clips);
            return true;
        }

        if (entry.AudioSource != null)
        {
            entry.AudioSource.clip = entry.OverrideClip;
            return true;
        }

        return false;
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        for (Type current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, AudioFieldFlags);
            if (field != null)
                return field;
        }

        return null;
    }
}
