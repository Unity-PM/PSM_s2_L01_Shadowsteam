using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public static class ComponentRegistry
{
    private static Dictionary<string, Type> _map;

    public static void Initialize()
    {
        _map = new();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<UIWidgetAttribute>();
                if (attr != null)
                {
                    _map[attr.WidgetTypeId] = type;
                    Debug.Log($"[Registry] registered: {attr.WidgetTypeId} → {type.Name}");
                }
            }
    }

    public static VisualElement Create(string typeId)
    {
        if (_map == null) Initialize();
        if (!_map.TryGetValue(typeId, out var type))
        {
            Debug.LogWarning($"[Registry] unknown typeId: '{typeId}'");
            return new VisualElement();
        }
        return (VisualElement)Activator.CreateInstance(type);
    }
}
