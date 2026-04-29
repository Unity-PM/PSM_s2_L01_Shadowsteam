using UnityEngine.UIElements;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PanelDefinitionSO _panelDef;
    [SerializeField] private StatComponent _player;

    private StatBar _hpBar, _mpBar, _stBar;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // one line builds the entire panel from the SO
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

    private void OnStatUpdated(StatUpdatedEvent e)
    {
        if (e.target != _player) return;
        Refresh();
    }

    private void Refresh()
    {
        _hpBar.SetValue(_player.getHP(), _player.getMaxHP());
        _mpBar.SetValue(_player.getMP(), _player.getMaxMP());
        _stBar.SetValue(_player.getStamina(), _player.getMaxStamina());
    }
}