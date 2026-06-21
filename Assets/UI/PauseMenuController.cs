using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[AddComponentMenu("UI/Pause Menu Controller")]
public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private bool toggleWithEscape = true;

    [Header("Text")]
    [SerializeField] private string titleText = "Paused";
    [SerializeField] private string subtitleText = "Game paused";
    [SerializeField] private string resumeText = "Resume";
    [SerializeField] private string mainMenuText = "Main Menu";
    [SerializeField] private string quitText = "Quit Game";

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Behaviour")]
    [SerializeField] private bool unlockCursorOnPause = true;
    [SerializeField] private bool lockCursorOnResume = true;
    [SerializeField] private bool pauseAudioListener;
    [SerializeField] private bool dontDestroyOnLoad;

    [Header("Style")]
    [SerializeField] private int titleFontSize = 42;
    [SerializeField] private int subtitleFontSize = 13;
    [SerializeField] private int buttonFontSize = 17;
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color panelColor = new Color(0.04f, 0.05f, 0.1f, 0.94f);
    [SerializeField] private Color accentColor = new Color(0.83f, 0.69f, 0.22f, 1f);
    [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.06f);
    [SerializeField] private Color buttonHoverColor = new Color(0.83f, 0.69f, 0.22f, 0.22f);
    [SerializeField] private Color buttonPressedColor = new Color(0.83f, 0.69f, 0.22f, 0.35f);

    private static PauseMenuController instance;

    private GameObject menuRoot;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool wasAudioListenerPaused;
    private bool isPaused;

    public static bool IsAnyPaused => instance != null && instance.isPaused;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (dontDestroyOnLoad)
            {
                Destroy(gameObject);
                return;
            }
        }

        instance = this;

        if (dontDestroyOnLoad)
        {
            // DontDestroyOnLoad only works on root objects; detach if nested.
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }

        BuildMenu();
        SetMenuVisible(false);
    }

    private void OnDisable()
    {
        if (isPaused)
            Resume();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (!toggleWithEscape || !WasEscapePressedThisFrame())
            return;

        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (isPaused)
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        wasAudioListenerPaused = AudioListener.pause;

        Time.timeScale = 0f;

        if (pauseAudioListener)
            AudioListener.pause = true;

        if (unlockCursorOnPause)
            UIManager.UnlockCursorForUi();

        EnsureEventSystemExists();
        SetMenuVisible(true);
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        AudioListener.pause = wasAudioListenerPaused;

        if (lockCursorOnResume)
            UIManager.LockCursorForGameplay();
        else
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }

        SetMenuVisible(false);
    }

    public void BackToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("PauseMenuController: 'Main Menu Scene Name' is empty.", this);
            return;
        }

        // Leaving the paused state: restore time/audio before loading so the menu scene runs normally.
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = wasAudioListenerPaused;

        UIManager.UnlockCursorForUi();

        SetMenuVisible(false);

        // Returning to the main menu must be a clean start. The player and the gameplay managers are
        // DontDestroyOnLoad (teleport/persistence system), so a plain scene load would leak them into the
        // menu where they keep falling (no floor) and then come back below the map -> "falling through
        // textures". Destroying them here makes the next game load spawn a fresh player.
        DestroyPersistentObjects();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void DestroyPersistentObjects()
    {
        // DontDestroyOnLoad objects live in a dedicated hidden scene. Create a probe, move it there,
        // then destroy every root object of that scene.
        GameObject probe = new GameObject("~DDOLProbe");
        DontDestroyOnLoad(probe);
        Scene persistentScene = probe.scene;

        GameObject[] roots = persistentScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i] != gameObject)
                Destroy(roots[i]);
        }

        // If this controller is itself persistent, don't let it survive into the menu either.
        if (gameObject.scene == persistentScene)
            Destroy(gameObject);
    }

    public void QuitGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = wasAudioListenerPaused;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenu()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        menuRoot = new GameObject("Pause Menu");
        menuRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform rootRect = menuRoot.AddComponent<RectTransform>();
        StretchToParent(rootRect);

        Image overlay = menuRoot.AddComponent<Image>();
        overlay.color = overlayColor;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(menuRoot.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(390f, 405f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        VerticalLayoutGroup layout = panelObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 28);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        // Let the panel grow to fit its content (title + subtitle + buttons) instead of a fixed height,
        // so adding/removing buttons or longer text never overflows the panel.
        ContentSizeFitter panelFitter = panelObject.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLabel(panelObject.transform, titleText, titleFontSize, accentColor, FontStyle.Bold, 56f);
        CreateLabel(panelObject.transform, subtitleText, subtitleFontSize, new Color(1f, 1f, 1f, 0.45f), FontStyle.Normal, 44f);
        CreateSpacer(panelObject.transform, 8f);
        CreateButton(panelObject.transform, resumeText, Resume);
        CreateButton(panelObject.transform, mainMenuText, BackToMainMenu);
        CreateButton(panelObject.transform, quitText, QuitGame);
    }

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot != null)
            menuRoot.SetActive(visible);
    }

    private Text CreateLabel(Transform parent, string text, int fontSize, Color color, FontStyle fontStyle, float height)
    {
        GameObject labelObject = new GameObject(text);
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = GetBuiltinFont();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;

        return label;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacerObject = new GameObject("Spacer");
        spacerObject.transform.SetParent(parent, false);

        LayoutElement layoutElement = spacerObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
    }

    private Button CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(text);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = buttonColor,
            highlightedColor = buttonHoverColor,
            pressedColor = buttonPressedColor,
            selectedColor = buttonHoverColor,
            disabledColor = new Color(1f, 1f, 1f, 0.02f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        button.onClick.AddListener(action);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 52f;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        StretchToParent(labelRect);

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = GetBuiltinFont();
        label.fontSize = buttonFontSize;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    private static void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static bool WasEscapePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
            return true;
#endif

        return false;
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
