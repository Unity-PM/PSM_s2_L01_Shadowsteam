using UnityEngine.UIElements;
using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    [SerializeField] private PanelDefinitionSO _panelDef;
    [SerializeField] private StyleSheet _stylesheet; // drag USS here

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (_stylesheet != null) root.styleSheets.Add(_stylesheet);
        UIFactory.Build(_panelDef, root);
    }
}