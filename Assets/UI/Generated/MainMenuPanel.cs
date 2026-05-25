using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuPanel : MonoBehaviour
{
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

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        if (stylesheet != null)
            root.styleSheets.Add(stylesheet);

        BindElements();
        RegisterCallbacks();

        settingsPanel.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void BindElements()
    {
        playBtn = root.Q<Button>("play-btn");
        settingsBtn = root.Q<Button>("settings-btn");
        exitBtn = root.Q<Button>("exit-btn");

        settingsPanel = root.Q<VisualElement>("settings-panel");
        closeSettingsBtn = root.Q<Button>("close-settings-btn");
    }

    private void RegisterCallbacks()
    {
        playBtn.clicked += OnPlayClicked;
        settingsBtn.clicked += OnSettingsClicked;
        exitBtn.clicked += OnExitClicked;

        closeSettingsBtn.clicked += OnCloseSettingsClicked;
    }

    private void UnregisterCallbacks()
    {
        playBtn.clicked -= OnPlayClicked;
        settingsBtn.clicked -= OnSettingsClicked;
        exitBtn.clicked -= OnExitClicked;

        closeSettingsBtn.clicked -= OnCloseSettingsClicked;
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnSettingsClicked()
    {
        settingsPanel.style.display = DisplayStyle.Flex;
    }

    private void OnCloseSettingsClicked()
    {
        settingsPanel.style.display = DisplayStyle.None;
    }

    private void OnExitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}