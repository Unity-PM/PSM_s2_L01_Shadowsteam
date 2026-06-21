using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Attach to any object to hide/show GameObjects or UI Toolkit documents with F1.
/// </summary>
public class HudVisibilityBinding : MonoBehaviour
{
    [SerializeField] private GameObject[] gameObjects;
    [SerializeField] private UIDocument[] uiDocuments;
    [SerializeField] private bool hideEntireGameObjects = true;

    void OnEnable()
    {
        GameplayHudVisibility.Register(ApplyVisibility);
    }

    void OnDisable()
    {
        GameplayHudVisibility.Unregister(ApplyVisibility);
    }

    void ApplyVisibility(bool visible)
    {
        ApplyGameObjects(visible);
        ApplyUiDocuments(visible);
    }

    void ApplyGameObjects(bool visible)
    {
        if (gameObjects == null)
            return;

        for (int i = 0; i < gameObjects.Length; i++)
        {
            GameObject go = gameObjects[i];
            if (go == null)
                continue;

            if (hideEntireGameObjects)
            {
                go.SetActive(visible);
                continue;
            }

            if (go.TryGetComponent(out UIDocument doc) && doc.rootVisualElement != null)
            {
                doc.rootVisualElement.style.visibility = visible
                    ? Visibility.Visible
                    : Visibility.Hidden;
            }
        }
    }

    void ApplyUiDocuments(bool visible)
    {
        if (uiDocuments == null)
            return;

        foreach (UIDocument doc in uiDocuments)
        {
            if (doc == null || doc.rootVisualElement == null)
                continue;

            doc.rootVisualElement.style.visibility = visible
                ? Visibility.Visible
                : Visibility.Hidden;
        }
    }
}
