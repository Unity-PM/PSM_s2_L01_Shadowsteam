using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class UIWidgetAttribute : Attribute
{
    public string WidgetTypeId { get; }
    public UIWidgetAttribute(string typeId) => WidgetTypeId = typeId;
}

// data Unity serializes per-component in the SO
[Serializable]
public class WidgetConfig
{
    public string typeId;    // matches [UIWidget("typeId")]
    public string name;
    public string ussClass;  // extra USS class to add
    public string value;     // generic init value (label text, etc)
    public Color color = Color.white;
}
