

using UnityEngine;
using UnityEngine.UIElements;

[UIWidget("stat-bar")]
public class StatBar : VisualElement, IConfigurable
{
    public new class UxmlFactory : UxmlFactory<StatBar, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlStringAttributeDescription _label =
            new() { name = "label", defaultValue = "HP" };
        UxmlColorAttributeDescription _color =
            new() { name = "fill-color", defaultValue = Color.red };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var bar = (StatBar)ve;
            bar.Label = _label.GetValueFromBag(bag, cc);
            bar.FillColor = _color.GetValueFromBag(bag, cc);
        }
    }

    private readonly Label _label;
    private readonly VisualElement _bg;
    private readonly VisualElement _fill;
    private readonly Label _valueLabel; // ← new

    public string Label
    {
        get => _label.text;
        set => _label.text = value;
    }

    public Color FillColor
    {
        get => _fill.resolvedStyle.backgroundColor;
        set => _fill.style.backgroundColor = value;
    }

    public void SetValue(float current, float max)
    {
        float pct = max > 0f ? current / max : 0f;
        _fill.style.width = Length.Percent(Mathf.Clamp01(pct) * 100f);

        // update text — round to int for clean display
        _valueLabel.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    public void Configure(WidgetConfig cfg)
    {
        Label = cfg.value;
        if (ColorUtility.TryParseHtmlString(cfg.colorHex, out var col))
            FillColor = col;
    }

    public StatBar()
    {
        AddToClassList("stat-bar");

        // left label (HP / MP / ST)
        _label = new Label();
        _label.AddToClassList("stat-bar__label");

        // background track
        _bg = new VisualElement();
        _bg.AddToClassList("stat-bar__bg");

        // fill bar
        _fill = new VisualElement();
        _fill.AddToClassList("stat-bar__fill");

        // value text overlay — sits on top of the bar
        _valueLabel = new Label();
        _valueLabel.AddToClassList("stat-bar__value");

        _bg.Add(_fill);
        _bg.Add(_valueLabel); // overlay inside the bg track

        Add(_label);
        Add(_bg);
    }
}