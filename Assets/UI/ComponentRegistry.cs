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
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Some assemblies fail to load every type; keep the ones that did.
                types = Array.FindAll(e.Types, t => t != null);
            }

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<UIWidgetAttribute>();
                if (attr == null)
                    continue;

                // Only concrete VisualElements can be instantiated as widgets.
                if (type.IsAbstract || !typeof(VisualElement).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"[Registry] '{type.Name}' is marked [UIWidget] but is not a concrete VisualElement; skipped.");
                    continue;
                }

                _map[attr.WidgetTypeId] = type;
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

        return Activator.CreateInstance(type) as VisualElement ?? new VisualElement();
    }
}
