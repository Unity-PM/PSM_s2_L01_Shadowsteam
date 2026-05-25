using UnityEngine;

public class UIManager : MonoBehaviour
{
    #region Singleton
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    #endregion

    #region Fields
    [Header("Always Visible — keep these active in Hierarchy")]
    [SerializeField] private GameObject _playerHudGO;
    [SerializeField] private GameObject _xpBarGO;
    [SerializeField] private GameObject _navbarGO;

    [Header("Toggle Panels — keep these INACTIVE in Hierarchy")]
    [SerializeField] private GameObject _inventoryGO;
    [SerializeField] private GameObject _questJournalGO;
    [SerializeField] private GameObject _characterScreenGO;

    private GameObject _currentOpenPanel;
    #endregion

    #region Lifecycle
    void Start()
    {
        // force-hide all toggle panels regardless of Hierarchy state
        ForceHide(_inventoryGO);
        ForceHide(_questJournalGO);
        ForceHide(_characterScreenGO);
    }
    #endregion

    #region Public API
    public void ToggleInventory() => Toggle(_inventoryGO);
    public void ToggleQuestJournal() => Toggle(_questJournalGO);
    public void ToggleCharacterScreen() => Toggle(_characterScreenGO);
    public void CloseAll()
    {
        if (_currentOpenPanel == null) return;
        _currentOpenPanel.SetActive(false);
        _currentOpenPanel = null;
    }
    #endregion

    #region Private
    private void Toggle(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIManager] panel not assigned — check Inspector.");
            return;
        }
        bool wasOpen = panel.activeSelf;
        CloseAll();
        if (!wasOpen)
        {
            panel.SetActive(true);
            _currentOpenPanel = panel;
        }
    }

    private void ForceHide(GameObject go)
    {
        if (go != null) go.SetActive(false);
    }
    #endregion
}