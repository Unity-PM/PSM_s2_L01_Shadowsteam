using UnityEngine.UIElements;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private StatComponent _player;

    private VisualElement _hpFill, _mpFill, _stFill;
    private Label _hpLabel, _mpLabel, _stLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _hpFill = root.Q("hp-bar-fill");
        _mpFill = root.Q("mp-bar-fill");
        _stFill = root.Q("st-bar-fill");
        _hpLabel = root.Q<Label>("hp-label");
        _mpLabel = root.Q<Label>("mp-label");
        _stLabel = root.Q<Label>("st-label");

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
        SetBar(_hpFill, _hpLabel, "HP", _player.getHP(), _player.getMaxHP());
        SetBar(_mpFill, _mpLabel, "MP", _player.getMP(), _player.getMaxMP());
        SetBar(_stFill, _stLabel, "ST", _player.getStamina(), _player.getMaxStamina());
    }

    private void SetBar(VisualElement fill, Label label,
                         string prefix, float cur, float max)
    {
        float pct = max > 0 ? cur / max : 0f;
        fill.style.width = Length.Percent(pct * 100f);
        label.text = prefix;
    }
}