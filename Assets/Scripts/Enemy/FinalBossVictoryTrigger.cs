using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(StatComponent))]
[AddComponentMenu("Enemy/Final Boss Victory Trigger")]
public class FinalBossVictoryTrigger : MonoBehaviour
{
    [Header("Victory Screen")]
    [SerializeField] private string victoryMessage = "Ты победил";
    [SerializeField, Min(0f)] private float exitDelay = 2.5f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(12)] private int fontSize = 72;

    [Header("Exit")]
    [SerializeField] private bool pauseGameOnVictory = true;
    [SerializeField] private string errorMessage = "You have beaten this god damn game, now you can forget it forever";

    private StatComponent stats;
    private bool triggered;

    private void Awake()
    {
        stats = GetComponent<StatComponent>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<DeathEvent>(OnDeath);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DeathEvent>(OnDeath);
    }

    private void OnDeath(DeathEvent deathEvent)
    {
        if (triggered || stats == null || deathEvent.target != stats)
            return;

        triggered = true;
        ShowVictoryScreen();

        if (pauseGameOnVictory)
            Time.timeScale = 0f;

        StartCoroutine(ExitWithError());
    }

    private void ShowVictoryScreen()
    {
        GameObject canvasObject = new GameObject("Final Boss Victory Screen");
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        StretchToParent(backgroundRect);

        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;

        GameObject textObject = new GameObject("Victory Text");
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        StretchToParent(textRect);
        textRect.offsetMin = new Vector2(80f, 80f);
        textRect.offsetMax = new Vector2(-80f, -80f);

        Text text = textObject.AddComponent<Text>();
        text.text = victoryMessage;
        text.font = GetBuiltinFont();
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private IEnumerator ExitWithError()
    {
        if (exitDelay > 0f)
            yield return new WaitForSecondsRealtime(exitDelay);

        Debug.LogError(errorMessage, this);

        if (pauseGameOnVictory)
            Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit(1);
#endif

        throw new InvalidOperationException(errorMessage);
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return font;
    }
}
