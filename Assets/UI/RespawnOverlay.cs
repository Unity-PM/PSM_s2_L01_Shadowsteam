using UnityEngine;
using UnityEngine.UIElements;

public class RespawnOverlay : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement overlay;
    private Label timerLabel;

    private float timer;
    private bool counting;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        overlay = root.Q<VisualElement>("respawn-overlay");
        timerLabel = root.Q<Label>("respawn-timer");

        overlay.style.display = DisplayStyle.None;

        EventBus.Subscribe<RespawnStartedEvent>(OnRespawnStarted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<RespawnStartedEvent>(OnRespawnStarted);
    }

    private void Update()
    {
        if (!counting)
            return;

        timer -= Time.deltaTime;

        int seconds = Mathf.CeilToInt(timer);

        timerLabel.text = $"Respawning in {seconds}...";

        if (timer <= 0f)
        {
            counting = false;
            overlay.style.display = DisplayStyle.None;
        }
    }

    private void OnRespawnStarted(RespawnStartedEvent e)
    {
        timer = e.RespawnDuration;

        overlay.style.display = DisplayStyle.Flex;

        counting = true;
    }
}