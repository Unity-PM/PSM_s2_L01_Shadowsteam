using System;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class UIWidgetAttribute : Attribute
{
    public string TypeId { get; }
    public UIWidgetAttribute(string typeId) => TypeId = typeId;
}

[Serializable]
public class WidgetConfig
{
    public string typeId;
    public string name;
    public string ussClass;
    public string value;
    public string colorHex = "#FFFFFF";
    public List<WidgetConfig> children; // nested widgets — null = leaf node
}