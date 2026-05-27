using System;
using System.Collections.Generic;

/// <summary>
/// Global gameplay HUD visibility (crosshair, optional overlays).
/// Register listeners via <see cref="Register"/> / <see cref="Unregister"/>.
/// </summary>
public static class GameplayHudVisibility
{
    static readonly List<Action<bool>> listeners = new();
    static bool isVisible = true;

    public static bool IsVisible => isVisible;

    public static event Action<bool> VisibilityChanged;

    public static void Register(Action<bool> listener)
    {
        if (listener == null || listeners.Contains(listener))
            return;

        listeners.Add(listener);
        listener(isVisible);
    }

    public static void Unregister(Action<bool> listener)
    {
        if (listener == null)
            return;

        listeners.Remove(listener);
    }

    public static void Toggle() => SetVisible(!isVisible);

    public static void SetVisible(bool visible)
    {
        if (isVisible == visible)
            return;

        isVisible = visible;

        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i]?.Invoke(isVisible);

        VisibilityChanged?.Invoke(isVisible);
    }
}
