using System;
using System.Collections.Generic;

public static class EventBus
{
    private static Dictionary<Type, Action<object>> events = new();

    private static Dictionary<Type, Dictionary<object, Action<object>>> _wrappers = new();

    public static void Subscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);
        if (!events.ContainsKey(type)) events[type] = delegate { };
        if (!_wrappers.ContainsKey(type)) _wrappers[type] = new();

        Action<object> wrapper = (e) => listener((T)e);
        _wrappers[type][listener] = wrapper;
        events[type] += wrapper;
    }

    public static void Unsubscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);
        if (_wrappers.TryGetValue(type, out var map) && map.TryGetValue(listener, out var wrapper))
        {
            events[type] -= wrapper;
            map.Remove(listener);
        }
    }

    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);

        if (events.ContainsKey(type))
            events[type].Invoke(eventData);
    }
}
