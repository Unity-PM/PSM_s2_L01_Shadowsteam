using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "NewPanel", menuName = "UI/Panel Definition")]
public class PanelDefinitionSO : ScriptableObject
{
    public string panelName;
    public string rootUssClass;
    public List<WidgetConfig> widgets;
}