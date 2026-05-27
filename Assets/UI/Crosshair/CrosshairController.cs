using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CrosshairController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement crosshairRoot;
    private Action<bool> visibilityHandler;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("CrosshairController requires UIDocument.", this);
            return;
        }

        crosshairRoot = uiDocument.rootVisualElement.Q<VisualElement>("crosshair-root");
        if (crosshairRoot == null)
        {
            Debug.LogError("CrosshairController could not find 'crosshair-root' in UXML.", this);
            return;
        }

        crosshairRoot.pickingMode = PickingMode.Ignore;

        visibilityHandler = ApplyVisibility;
        GameplayHudVisibility.Register(visibilityHandler);
    }

    void OnDisable()
    {
        if (visibilityHandler != null)
            GameplayHudVisibility.Unregister(visibilityHandler);
    }

    void ApplyVisibility(bool visible)
    {
        if (crosshairRoot == null)
            return;

        crosshairRoot.style.visibility = visible
            ? Visibility.Visible
            : Visibility.Hidden;
    }
}
