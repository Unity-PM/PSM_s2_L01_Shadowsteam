using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static Dictionary<Type, Delegate> events = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Clear() => events.Clear();

    public static void Subscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);
        if (!events.ContainsKey(type)) events[type] = null;
        events[type] = Delegate.Combine(events[type], listener);
    }

    public static void Unsubscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);
        if (events.ContainsKey(type)) events[type] = Delegate.Remove(events[type], listener);
    }

    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);
        if (events.TryGetValue(type, out Delegate del) && del != null)
        {
            // Динамический вызов (так проще всего в KISS стиле для статики)
            ((Action<T>)del).Invoke(eventData);
        }
    }
}