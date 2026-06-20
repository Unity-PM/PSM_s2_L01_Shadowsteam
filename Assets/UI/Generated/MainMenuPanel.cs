using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuPanel : MonoBehaviour
{
    private const string MasterVolumePrefKey = "MasterVolume";

    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet stylesheet;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Main_Scene";

    private VisualElement root;

    private Button playBtn;
    private Button settingsBtn;
    private Button exitBtn;

    private VisualElement settingsPanel;
    private Button closeSettingsBtn;
    private Slider volumeSlider;
    private Label volumeValueLabel;

    private Coroutine bindCoroutine;
    private bool callbacksRegistered;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        UnlockCursor();
        EnsureEventSystem();
        ApplyVolume(PlayerPrefs.GetFloat(MasterVolumePrefKey, 1f), false);
    }

    private void OnEnable()
    {
        UnlockCursor();
        EnsureEventSystem();
        StartBinding();
    }

    private void OnDisable()
    {
        if (bindCoroutine != null)
        {
            StopCoroutine(bindCoroutine);
            bindCoroutine = null;
        }

        UnregisterCallbacks();
    }

    private void StartBinding()
    {
        if (bindCoroutine != null)
            StopCoroutine(bindCoroutine);

        bindCoroutine = StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        for (int i = 0; i < 10; i++)
        {
            if (TryBind())
            {
                bindCoroutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[MainMenuPanel] UI elements were not found. Check MainMenuPanel.uxml names.", this);
        bindCoroutine = null;
    }

    private bool TryBind()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
            return false;

        playBtn = root.Q<Button>("play-btn");
        settingsBtn = root.Q<Button>("settings-btn");
        exitBtn = root.Q<Button>("exit-btn");

        settingsPanel = root.Q<VisualElement>("settings-panel");
        closeSettingsBtn = root.Q<Button>("close-settings-btn");
        volumeSlider = root.Q<Slider>("volume-slider");
        volumeValueLabel = root.Q<Label>("volume-value-label");

        if (playBtn == null || settingsBtn == null || exitBtn == null)
            return false;

        if (stylesheet != null)
            root.styleSheets.Add(stylesheet);

        RegisterCallbacks();

        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.None;

        SyncVolumeUi();
        return true;
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered)
            return;

        playBtn.clicked += OnPlayClicked;
        settingsBtn.clicked += OnSettingsClicked;
        exitBtn.clicked += OnExitClicked;

        if (closeSettingsBtn != null)
            closeSettingsBtn.clicked += OnCloseSettingsClicked;

        if (volumeSlider != null)
            volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);

        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered)
            return;

        if (playBtn != null)
            playBtn.clicked -= OnPlayClicked;

        if (settingsBtn != null)
            settingsBtn.clicked -= OnSettingsClicked;

        if (exitBtn != null)
            exitBtn.clicked -= OnExitClicked;

        if (closeSettingsBtn != null)
            closeSettingsBtn.clicked -= OnCloseSettingsClicked;

        if (volumeSlider != null)
            volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);

        callbacksRegistered = false;
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.Flex;
    }

    private void OnCloseSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.style.display = DisplayStyle.None;
    }

    private void OnExitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        ApplyVolume(evt.newValue, true);
    }

    private void ApplyVolume(float volume, bool save)
    {
        float normalizedVolume = Mathf.Clamp01(volume);
        AudioListener.volume = normalizedVolume;

        if (save)
        {
            PlayerPrefs.SetFloat(MasterVolumePrefKey, normalizedVolume);
            PlayerPrefs.Save();
        }

        UpdateVolumeLabel(normalizedVolume);
    }

    private void SyncVolumeUi()
    {
        float volume = Mathf.Clamp01(AudioListener.volume);

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(volume);

        UpdateVolumeLabel(volume);
    }

    private void UpdateVolumeLabel(float volume)
    {
        if (volumeValueLabel != null)
            volumeValueLabel.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    private void EnsureEventSystem()
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

    private static void UnlockCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
}
