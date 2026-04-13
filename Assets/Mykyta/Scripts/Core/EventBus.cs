using System;
using System.Collections.Generic;

public static class EventBus
{
    private static Dictionary<Type, Action<object>> events = new();

    public static void Subscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);

        if (!events.ContainsKey(type))
            events[type] = delegate { };

        events[type] += (e) => listener((T)e);
    }

    public static void Unsubscribe<T>(Action<T> listener)
    {
        Type type = typeof(T);

        if (events.ContainsKey(type))
            events[type] -= (e) => listener((T)e);
    }

    public static void Publish<T>(T eventData)
    {
        Type type = typeof(T);

        if (events.ContainsKey(type))
            events[type].Invoke(eventData);
    }
}
