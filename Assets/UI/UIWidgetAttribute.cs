using System;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class UIWidgetAttribute : Attribute
{
    public string WidgetTypeId { get; }
    public UIWidgetAttribute(string typeId) => WidgetTypeId = typeId;
}

[Serializable]
public class WidgetConfig
{
    public string typeId;
    public string name;
    public string ussClass;
    public string value;
    public string colorHex = "#FFFFFF";
    [SerializeReference] public List<WidgetConfig> children; // nested widgets - null = leaf node
}
