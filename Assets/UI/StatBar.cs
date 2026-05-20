using UnityEngine;
using UnityEngine.UIElements;

[UIWidget("stat-bar")]
public class StatBar : VisualElement, IConfigurable
{
    // ── UxmlFactory lets Unity see this in UXML and the UI Builder ──
    [System.Obsolete]
    public new class UxmlFactory : UxmlFactory<StatBar, UxmlTraits> { }

    [System.Obsolete]
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        // attributes you can set directly in UXML
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

    // ── internal elements ──
    private readonly Label _label;
    private readonly VisualElement _fill;

    // ── public API ──
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
    }

    // ── constructor — builds the internal tree ──
    public StatBar()
    {
        AddToClassList("stat-bar");

        _label = new Label();
        _label.AddToClassList("stat-bar__label");

        var bg = new VisualElement();
        bg.AddToClassList("stat-bar__bg");

        _fill = new VisualElement();
        _fill.AddToClassList("stat-bar__fill");

        bg.Add(_fill);
        Add(_label);
        Add(bg);
    }

    public void Configure(WidgetConfig cfg)
    {
        Label = cfg.value;
        if (ColorUtility.TryParseHtmlString(cfg.colorHex, out var col))
            FillColor = col;
    }
}