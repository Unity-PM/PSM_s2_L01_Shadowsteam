

using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
[UIWidget("stat-bar")]
public partial class StatBar : VisualElement, IConfigurable
{
    private readonly Label _label;
    private readonly VisualElement _bg;
    private readonly VisualElement _fill;
    private readonly Label _valueLabel;

    private string _labelText = "HP";
    private Color _fillColor = Color.red;

    [UxmlAttribute("label")]
    public string Label
    {
        get => _labelText;
        set
        {
            _labelText = value;
            if (_label != null)
                _label.text = value;
        }
    }

    [UxmlAttribute("fill-color")]
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            if (_fill != null)
                _fill.style.backgroundColor = value;
        }
    }

    public void SetValue(float current, float max)
    {
        float pct = max > 0f ? current / max : 0f;
        _fill.style.width = Length.Percent(Mathf.Clamp01(pct) * 100f);

        // update text - round to int for clean display
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
        _label = new Label(_labelText);
        _label.AddToClassList("stat-bar__label");

        // background track
        _bg = new VisualElement();
        _bg.AddToClassList("stat-bar__bg");

        // fill bar
        _fill = new VisualElement();
        _fill.AddToClassList("stat-bar__fill");
        _fill.style.backgroundColor = _fillColor;

        // value text overlay - sits on top of the bar
        _valueLabel = new Label();
        _valueLabel.AddToClassList("stat-bar__value");

        _bg.Add(_fill);
        _bg.Add(_valueLabel); // overlay inside the bg track

        Add(_label);
        Add(_bg);
    }
}
