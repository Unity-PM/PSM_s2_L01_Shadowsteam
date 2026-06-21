using UnityEngine.UIElements;

[UIWidget("gold-label")]
public class GoldLabel : VisualElement, IConfigurable
{
    private readonly Label _label;

    public GoldLabel()
    {
        AddToClassList("gold-label");
        _label = new Label("0");
        _label.AddToClassList("gold-label__text");
        Add(_label);
    }

    public void Configure(WidgetConfig cfg)
    {
        _label.text = cfg.value;
    }

    public void SetValue(int amount) => _label.text = $"{amount:N0}g";
}
