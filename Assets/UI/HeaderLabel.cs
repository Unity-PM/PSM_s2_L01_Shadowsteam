using UnityEngine.UIElements;

[UIWidget("header")]
public class HeaderLabel : VisualElement, IConfigurable
{
    private readonly Label _label;

    public HeaderLabel()
    {
        AddToClassList("header-label");
        _label = new Label();
        _label.AddToClassList("header-label__text");
        Add(_label);
    }

    public void Configure(WidgetConfig cfg)
    {
        _label.text = cfg.value;
    }
}