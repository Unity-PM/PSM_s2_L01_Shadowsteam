using UnityEngine.UIElements;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PanelDefinitionSO _panelDef;
    [SerializeField] private StatComponent _player;

    StatBar _hpBar, _mpBar, _stBar;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || _panelDef == null || _player == null)
        {
            Debug.LogError("PlayerHUD is missing UIDocument, panel definition, or player stats.", this);
            return;
        }

        var root = uiDocument.rootVisualElement;
        var panel = UIFactory.Build(_panelDef, root);

        _hpBar = panel.Q<StatBar>("_hpBar");
        _mpBar = panel.Q<StatBar>("_mpBar");
        _stBar = panel.Q<StatBar>("_stBar");

        EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);
        Refresh();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<StatUpdatedEvent>(OnStatUpdated);
    }

    void OnStatUpdated(StatUpdatedEvent e)
    {
        if (e.target != _player) return;
        Refresh();
    }

    void Refresh()
    {
        if (_player == null || _hpBar == null || _mpBar == null || _stBar == null)
            return;

        _hpBar.SetValue(_player.getHP(), _player.getMaxHP());
        _mpBar.SetValue(_player.getMP(), _player.getMaxMP());
        _stBar.SetValue(_player.getStamina(), _player.getMaxStamina());
    }
}
