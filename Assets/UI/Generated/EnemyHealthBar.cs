using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset hpBarTemplate;
    [SerializeField] private StatComponent stats;

    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    private Camera cam;

    private TemplateContainer root;

    private VisualElement container;
    private VisualElement hpFill;
    private Label hpText;

    private const float BAR_WIDTH = 90f;

    private void Start()
    {
        cam = Camera.main;

        root = hpBarTemplate.Instantiate();
        root.style.position = Position.Absolute;
        uiDocument.rootVisualElement.Add(root);

        container = root.Q<VisualElement>(className: "enemy-hp");

        hpFill = root.Q<VisualElement>("hp-fill");
        hpText = root.Q<Label>("hp-text");

        Refresh();

        EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);

        StartCoroutine(RefreshNextFrame());
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<StatUpdatedEvent>(OnStatUpdated);

        root?.RemoveFromHierarchy();
    }

    private void LateUpdate()
    {
        if (cam == null || container == null)
            return;

        Vector3 worldPos = transform.position + worldOffset;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0)
        {
            container.style.display = DisplayStyle.None;
            return;
        }

        container.style.display = DisplayStyle.Flex;

        container.style.left = screenPos.x - BAR_WIDTH * 0.5f;

        container.style.top = Screen.height - screenPos.y;
    }

    private void OnStatUpdated(StatUpdatedEvent e)
    {
        if (e.target != stats)
            return;

        Refresh();
    }

    private void Refresh()
    {
        float current = stats.getHP();
        float max = stats.getMaxHP();

        float percent = max > 0 ? current / max : 0f;

        hpFill.style.width = Length.Percent(percent * 100f);

        hpText.text =
            $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        Refresh();
    }
}