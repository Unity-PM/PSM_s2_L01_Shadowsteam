using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Stats")]
    [SerializeField] private StatComponent stats;

    [Header("Anchor")]
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private bool useRendererBounds = false;
    [SerializeField] private float boundsTopPadding = 0.15f;

    [Header("Distance Scaling")]
    [SerializeField] private float referenceDistance = 5f;
    [SerializeField] private float minScale = 0.4f;
    [SerializeField] private float maxScale = 2.0f;

    [Header("Fade")]
    [SerializeField] private float fadeStartDistance = 14f;
    [SerializeField] private float fadeEndDistance = 20f;

    [Header("Auto-hide")]
    [SerializeField] private float visibleDuration = 3f;

    [Header("Occlusion")]
    [SerializeField] private LayerMask occlusionMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float occlusionRayInset = 0.08f;

    private Camera cam;
    private Transform enemyRoot;
    private Renderer[] renderers;
    private CanvasGroup canvasGroup;

    private float lastDamageTime = float.NegativeInfinity;
    private bool isFullHealth = true;
    private bool hasBeenDamaged;

    private void Awake()
    {
        canvasGroup = worldCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = worldCanvas.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        HideImmediate();
    }

    private void Start()
    {
        cam = Camera.main;
        enemyRoot = transform;
        renderers = GetComponentsInChildren<Renderer>();

        if (stats == null)
            stats = GetComponentInParent<StatComponent>() ?? GetComponentInChildren<StatComponent>();

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = cam;

        UpdateCanvasPosition();
        EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);
        StartCoroutine(InitRefreshRoutine());
    }

    private IEnumerator InitRefreshRoutine()
    {
        yield return null;
        Refresh();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<StatUpdatedEvent>(OnStatUpdated);
    }

    private void LateUpdate()
    {
        UpdateCanvasPosition();
        UpdateVisibility();
    }

    private void UpdateCanvasPosition()
    {
        Vector3 worldPos = GetAnchorWorldPosition();
        worldCanvas.transform.position = worldPos;

        if (cam != null)
        {
            Vector3 dir = worldPos - cam.transform.position;
            if (dir != Vector3.zero)
                worldCanvas.transform.rotation = Quaternion.LookRotation(dir);
        }

        if (cam != null)
        {
            float dist = Vector3.Distance(cam.transform.position, worldPos);
            float scale = Mathf.Clamp(dist / referenceDistance, minScale, maxScale);
            worldCanvas.transform.localScale = Vector3.one * scale;
        }
    }

    private void UpdateVisibility()
    {
        if (cam == null) { HideImmediate(); return; }
        if (stats == null || stats.IsDead) { HideImmediate(); return; }
        if (isFullHealth && hasBeenDamaged && Time.time - lastDamageTime > visibleDuration)
        {
            HideImmediate();
            return;
        }

        Vector3 worldPos = worldCanvas.transform.position;

        Vector3 toTarget = worldPos - cam.transform.position;
        if (Vector3.Dot(cam.transform.forward, toTarget) <= 0f) { HideImmediate(); return; }

        if (!IsVisibleFromCamera(worldPos)) { HideImmediate(); return; }

        float dist = toTarget.magnitude;
        float distAlpha = 1f - Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, dist);

        float timeSinceDamage = Time.time - lastDamageTime;
        float timeAlpha = 1f;
        if (isFullHealth && hasBeenDamaged)
            timeAlpha = Mathf.Clamp01(1f - Mathf.InverseLerp(visibleDuration * 0.6f, visibleDuration, timeSinceDamage));

        canvasGroup.alpha = distAlpha * timeAlpha;
    }

    private void HideImmediate()
    {
        canvasGroup.alpha = 0f;
    }

    private bool IsVisibleFromCamera(Vector3 worldPos)
    {
        Vector3 origin = cam.transform.position;
        Vector3 toTarget = worldPos - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.01f) return true;

        Vector3 direction = toTarget / distance;
        float rayDist = Mathf.Max(0f, distance - occlusionRayInset);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, rayDist, occlusionMask, QueryTriggerInteraction.Ignore))
            return true;

        return hit.transform == enemyRoot || hit.transform.IsChildOf(enemyRoot);
    }

    private Vector3 GetAnchorWorldPosition()
    {
        if (headAnchor != null)
            return headAnchor.position;

        if (useRendererBounds && renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return new Vector3(b.center.x, b.max.y + boundsTopPadding, b.center.z);
        }

        return transform.TransformPoint(localOffset);
    }

    private void OnStatUpdated(StatUpdatedEvent e)
    {
        if (e.target != stats) return;

        Refresh();

        if (!isFullHealth)
        {
            hasBeenDamaged = true;
            lastDamageTime = Time.time;
        }
    }

    private void Refresh()
    {
        float current = stats.getHP();
        float max = stats.getMaxHP();
        float percent = max > 0f ? current / max : 0f;

        hpFillImage.fillAmount = percent;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

        isFullHealth = Mathf.Approximately(percent, 1f);
    }
}
