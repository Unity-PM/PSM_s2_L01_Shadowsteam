using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIFactory
{
    public static VisualElement Build(PanelDefinitionSO def, VisualElement container)
    {
        var root = new VisualElement();
        root.name = def.panelName;

        if (!string.IsNullOrEmpty(def.rootUssClass))
            root.AddToClassList(def.rootUssClass);

        BuildChildren(def.widgets, root);

        container.Add(root);
        return root;
    }

    // recursively spawn a list of WidgetConfigs into a parent element
    private static void BuildChildren(List<WidgetConfig> configs, VisualElement parent)
    {
        if (configs == null) return;

        foreach (var cfg in configs)
        {
            var widget = SpawnWidget(cfg);
            parent.Add(widget);

            // recurse into children if this is a container
            if (cfg.children != null && cfg.children.Count > 0)
                BuildChildren(cfg.children, widget);
        }
    }

    private static VisualElement SpawnWidget(WidgetConfig cfg)
    {
        VisualElement widget;

        // "container" is a built-in type — just a plain VisualElement, no registry needed
        if (cfg.typeId == "container")
        {
            widget = new VisualElement();
        }
        else
        {
            widget = ComponentRegistry.Create(cfg.typeId);
        }

        widget.name = cfg.name;

        if (!string.IsNullOrEmpty(cfg.ussClass))
            foreach (var cls in cfg.ussClass.Split(' '))
                if (!string.IsNullOrEmpty(cls)) widget.AddToClassList(cls);

        if (widget is IConfigurable c)
            c.Configure(cfg);

        return widget;
    }
}

public interface IConfigurable
{
    void Configure(WidgetConfig cfg);
}