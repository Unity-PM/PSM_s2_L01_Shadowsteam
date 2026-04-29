using UnityEngine.UIElements;

public static class UIFactory
{
    public static VisualElement Build(PanelDefinitionSO def, VisualElement container)
    {
        var root = new VisualElement();
        root.name = def.panelName;
        if (!string.IsNullOrEmpty(def.rootUssClass))
            root.AddToClassList(def.rootUssClass);

        foreach (var cfg in def.widgets)
        {
            var widget = ComponentRegistry.Create(cfg.typeId);
            widget.name = cfg.name;

            if (!string.IsNullOrEmpty(cfg.ussClass))
                widget.AddToClassList(cfg.ussClass);

            // let widgets opt-in to config via interface
            if (widget is IConfigurable c)
                c.Configure(cfg);

            root.Add(widget);
        }

        container.Add(root);
        return root;
    }
}

// optional interface — implement on any widget that needs config data
public interface IConfigurable
{
    void Configure(WidgetConfig cfg);
}