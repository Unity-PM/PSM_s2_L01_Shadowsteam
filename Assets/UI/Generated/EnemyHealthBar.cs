using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset hpBarTemplate;
    [SerializeField] private StatComponent stats;

    [Header("Anchor")]
    [SerializeField] private Transform headAnchor;
    [FormerlySerializedAs("worldOffset")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool useRendererBounds;
    [SerializeField] private float boundsTopPadding = 0.15f;

    [Header("Visibility")]
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float occlusionRayInset = 0.08f;

    private Camera cam;
    private Transform enemyRoot;
    private Renderer[] renderers;

    private TemplateContainer root;

    private VisualElement hpFill;
    private Label hpText;

    private const float BAR_WIDTH = 90f;
    private const float BAR_HEIGHT = 18f;

    private void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    private void Start()
    {
        cam = Camera.main;
        enemyRoot = transform;
        renderers = GetComponentsInChildren<Renderer>();

        root = hpBarTemplate.Instantiate();
        root.style.position = Position.Absolute;
        root.style.width = BAR_WIDTH;
        root.style.height = BAR_HEIGHT;
        root.pickingMode = PickingMode.Ignore;
        uiDocument.rootVisualElement.Add(root);

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

    private void OnBeforeRender()
    {
        UpdateBarPosition();
    }

    private void UpdateBarPosition()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null || root == null || root.panel == null || stats == null)
            return;

        if (stats.IsDead)
        {
            HideBar();
            return;
        }

        Vector3 worldPos = GetAnchorWorldPosition();

        if (!IsVisibleFromCamera(worldPos))
        {
            HideBar();
            return;
        }

        Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);
        if (viewportPos.z <= 0f
            || viewportPos.x < 0f || viewportPos.x > 1f
            || viewportPos.y < 0f || viewportPos.y > 1f)
        {
            HideBar();
            return;
        }

        VisualElement panelRoot = root.panel.visualTree;
        float panelWidth = panelRoot.layout.width;
        float panelHeight = panelRoot.layout.height;
        if (panelWidth <= 0f || panelHeight <= 0f)
            return;

        root.style.visibility = Visibility.Visible;
        root.style.left = viewportPos.x * panelWidth - BAR_WIDTH * 0.5f;
        root.style.top = (1f - viewportPos.y) * panelHeight - BAR_HEIGHT * 0.5f;
    }

    private Vector3 GetAnchorWorldPosition()
    {
        if (headAnchor != null)
            return headAnchor.position;

        if (useRendererBounds && renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return new Vector3(bounds.center.x, bounds.max.y + boundsTopPadding, bounds.center.z);
        }

        return transform.TransformPoint(localOffset);
    }

    private bool IsVisibleFromCamera(Vector3 worldPos)
    {
        Vector3 origin = cam.transform.position;
        Vector3 toTarget = worldPos - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.01f)
            return true;

        Vector3 direction = toTarget / distance;
        float rayDistance = Mathf.Max(0f, distance - occlusionRayInset);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance, occlusionMask, QueryTriggerInteraction.Ignore))
            return true;

        Transform hitTransform = hit.transform;
        return hitTransform == enemyRoot || hitTransform.IsChildOf(enemyRoot);
    }

    private void HideBar()
    {
        root.style.visibility = Visibility.Hidden;
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
