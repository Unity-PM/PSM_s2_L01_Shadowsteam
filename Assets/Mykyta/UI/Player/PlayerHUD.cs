using UnityEngine.UIElements;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private StatComponent _player;

    private StatBar _hpBar, _mpBar, _stBar;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _hpBar = root.Q<StatBar>("_hpBar");
        _mpBar = root.Q<StatBar>("_mpBar");
        _stBar = root.Q<StatBar>("_stBar");

        EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);
        Refresh();

        Debug.Log($"hp:{_hpBar} mp:{_mpBar} st:{_stBar}");
        Debug.Log($"hp childCount:{_hpBar?.childCount}");
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