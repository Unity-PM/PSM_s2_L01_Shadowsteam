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
    [Header("Always Visible � keep these active in Hierarchy")]
    [SerializeField] private GameObject _playerHudGO;
    [SerializeField] private GameObject _xpBarGO;
    [SerializeField] private GameObject _navbarGO;

    [Header("Toggle Panels � keep these INACTIVE in Hierarchy")]
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
    public static void UnlockCursorForUi()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void LockCursorForGameplay()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleInventory() => Toggle(_inventoryGO);
    public void ToggleQuestJournal() => Toggle(_questJournalGO);
    public void ToggleCharacterScreen() => Toggle(_characterScreenGO);
    public void CloseAll()
    {
        if (_currentOpenPanel == null)
            return;

        _currentOpenPanel.SetActive(false);
        _currentOpenPanel = null;
        LockCursorForGameplay();
    }
    #endregion

    #region Private
    private void Toggle(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIManager] Panel not assigned � check Inspector.", this);
            return;
        }

        bool wasOpen = panel.activeSelf;
        CloseAll();
        if (!wasOpen)
        {
            UnlockCursorForUi();
            panel.SetActive(true);
            _currentOpenPanel = panel;
            if (panel == _questJournalGO && panel.TryGetComponent(out QuestJournalPanel questJournal))
                questJournal.Refresh();
            else if (panel == _questJournalGO)
                Debug.LogWarning("[UIManager] QuestJournalPanel missing on QuestJournalUI.", this);
            else if (panel == _inventoryGO && panel.TryGetComponent(out InventoryPanel inventoryPanel))
                inventoryPanel.Refresh();
            else if (panel == _characterScreenGO && panel.TryGetComponent(out CharacterPanel characterPanel))
                characterPanel.Refresh();
        }
    }

    private void ForceHide(GameObject go)
    {
        if (go != null) go.SetActive(false);
    }
    #endregion
}
