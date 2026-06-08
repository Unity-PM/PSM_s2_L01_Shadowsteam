using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(100)]
public class CharacterPanel : MonoBehaviour
{
	#region Fields
	[SerializeField] private StatComponent playerStats;

	private UIDocument uiDocument;
	private VisualElement statsList;
	private Button closeBtn;

	EventCallback<ClickEvent> closeClickCallback;

	static readonly HashSet<StatType> PercentageStats = new()
	{
		StatType.CritChance,
		StatType.CritDamage,
		StatType.DodgeChance,
		StatType.BlockChance,
		StatType.CooldownReduction
	};

	static readonly StatType[] VitalStats =
	{
		StatType.HP,
		StatType.MP,
		StatType.Stamina
	};

	static readonly StatType[] AttributeStats =
	{
		StatType.HPRegen,
		StatType.MPRegen,
		StatType.StaminaRegen,
		StatType.ATK,
		StatType.MAG,
		StatType.DEF,
		StatType.MDEF,
		StatType.CritChance,
		StatType.CritDamage,
		StatType.MS,
		StatType.AS,
		StatType.DodgeChance,
		StatType.BlockChance,
		StatType.CooldownReduction
	};
	#endregion

	#region Debug
	// #region agent log
	const string DebugLogPath = "/Users/misakostenko/Desktop/Politechnika/Unity/PSM_s2_L01_Shadowsteam/.cursor/debug-34eaa1.log";

	static void AgentLog(string hypothesisId, string location, string message, string dataJson)
	{
		try
		{
			var line = $"{{\"sessionId\":\"34eaa1\",\"hypothesisId\":\"{hypothesisId}\",\"location\":\"{location}\",\"message\":\"{message}\",\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
			File.AppendAllText(DebugLogPath, line);
		}
		catch { }
	}

	static string JoinChildNames(VisualElement root)
	{
		if (root == null)
			return "null";

		var sb = new StringBuilder();
		for (int i = 0; i < root.childCount; i++)
		{
			if (i > 0)
				sb.Append(',');

			var child = root[i];
			sb.Append(string.IsNullOrEmpty(child.name) ? child.GetType().Name : child.name);
		}

		return sb.ToString();
	}
	// #endregion
	#endregion

	#region Lifecycle
	void OnEnable()
	{
		EventBus.Subscribe<StatUpdatedEvent>(OnStatUpdated);

		if (uiDocument == null)
			uiDocument = GetComponent<UIDocument>();

		var root = uiDocument != null ? uiDocument.rootVisualElement : null;
		var vta = uiDocument != null ? uiDocument.visualTreeAsset : null;

		// #region agent log
		AgentLog("H-A", "CharacterPanel.OnEnable", "enable",
			$"{{\"vtaNull\":{(vta == null).ToString().ToLower()},\"vtaName\":\"{(vta != null ? vta.name : "null")}\",\"rootNull\":{(root == null).ToString().ToLower()},\"rootChildCount\":{(root != null ? root.childCount : -1)}}}");
		// #endregion

		if (root == null)
		{
			// #region agent log
			AgentLog("H-B", "CharacterPanel.OnEnable", "root null immediate bind", "{}");
			// #endregion
			TryBindUi();
			return;
		}

		// #region agent log
		AgentLog("H-B", "CharacterPanel.OnEnable", "deferred bind scheduled", "{}");
		// #endregion
		root.schedule.Execute(TryBindUi).ExecuteLater(0);
	}

	void OnDisable()
	{
		EventBus.Unsubscribe<StatUpdatedEvent>(OnStatUpdated);
		UnbindButtons();
	}

	void OnStatUpdated(StatUpdatedEvent e)
	{
		if (playerStats == null)
			ResolvePlayerStats();

		if (playerStats == null || e.target != playerStats)
			return;

		if (isActiveAndEnabled)
			Refresh();
	}
	#endregion

	#region Binding
	void TryBindUi()
	{
		UnbindButtons();

		if (uiDocument == null)
			uiDocument = GetComponent<UIDocument>();

		var root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogWarning("[CharacterPanel] rootVisualElement null (UIDocument not ready?).", this);
			return;
		}

		statsList = root.Q<VisualElement>("stats-list");
		closeBtn = root.Q<Button>("close-btn");

		// #region agent log
		AgentLog("H-C", "CharacterPanel.TryBindUi", "bind result",
			$"{{\"vtaNull\":{(uiDocument.visualTreeAsset == null).ToString().ToLower()},\"vtaName\":\"{(uiDocument.visualTreeAsset != null ? uiDocument.visualTreeAsset.name : "null")}\",\"rootChildCount\":{root.childCount},\"childNames\":\"{JoinChildNames(root)}\",\"statsListFound\":{(statsList != null).ToString().ToLower()},\"closeBtnFound\":{(closeBtn != null).ToString().ToLower()}}}");
		// #endregion

		if (statsList == null)
			Debug.LogWarning("[CharacterPanel] stats-list not found in UXML.", this);

		if (closeBtn == null)
			Debug.LogWarning("[CharacterPanel] close-btn not found in UXML.", this);
		else
			BindButton(closeBtn, OnCloseBtnClicked, ref closeClickCallback);

		if (statsList != null && isActiveAndEnabled)
			Refresh();

		ResolvePlayerStats();
	}

	static void BindButton(Button button, Action handler, ref EventCallback<ClickEvent> clickCallback)
	{
		clickCallback = _ => handler();
		button.RegisterCallback(clickCallback);
	}

	void UnbindButtons()
	{
		if (closeBtn != null && closeClickCallback != null)
		{
			closeBtn.UnregisterCallback(closeClickCallback);
			closeClickCallback = null;
		}

		closeBtn = null;
	}

	void ResolvePlayerStats()
	{
		if (playerStats != null)
			return;

		var playerGo = GameObject.FindGameObjectWithTag("Player");
		if (playerGo != null && playerGo.TryGetComponent(out StatComponent stats))
		{
			playerStats = stats;
			return;
		}

		var skillInput = FindFirstObjectByType<PlayerSkillInput>();
		if (skillInput != null && skillInput.TryGetComponent(out stats))
			playerStats = stats;
	}
	#endregion

	#region Handlers
	void OnCloseBtnClicked()
	{
		if (UIManager.Instance != null)
			UIManager.Instance.CloseAll();
		else
		{
			gameObject.SetActive(false);
			UIManager.LockCursorForGameplay();
		}
	}
	#endregion

	#region Data
	public void Refresh()
	{
		// #region agent log
		AgentLog("H-B", "CharacterPanel.Refresh", "refresh called",
			$"{{\"statsListNull\":{(statsList == null).ToString().ToLower()}}}");
		// #endregion

		if (statsList == null)
		{
			TryBindUi();
			if (statsList == null)
				return;
		}

		ResolvePlayerStats();
		statsList.Clear();

		if (playerStats == null)
		{
			AddHint("StatComponent not found in scene.");
			return;
		}

		AddSectionLabel("Vitals");
		foreach (StatType type in VitalStats)
			AddStatRow(type, includeCurrent: true);

		AddSectionLabel("Attributes");
		foreach (StatType type in AttributeStats)
			AddStatRow(type, includeCurrent: false);
	}

	void AddSectionLabel(string text)
	{
		var label = new Label(text);
		label.AddToClassList("stat-section-label");
		statsList.Add(label);
	}

	void AddStatRow(StatType type, bool includeCurrent)
	{
		float baseVal = playerStats.GetBaseStat(type);
		float mod = playerStats.GetStatModifier(type);
		string display = includeCurrent
			? FormatVitalLine(type, baseVal, mod)
			: FormatAttributeLine(type, baseVal, mod);

		var row = new VisualElement();
		row.AddToClassList("stat-row");

		var nameLabel = new Label(FormatStatName(type));
		nameLabel.AddToClassList("stat-row__name");

		var valueLabel = new Label(display);
		valueLabel.enableRichText = true;
		valueLabel.AddToClassList("stat-row__value");

		row.Add(nameLabel);
		row.Add(valueLabel);
		statsList.Add(row);
	}

	void AddHint(string text)
	{
		var hint = new Label(text);
		hint.AddToClassList("empty-hint");
		statsList.Add(hint);
	}

	string FormatVitalLine(StatType type, float baseVal, float mod)
	{
		float current = type switch
		{
			StatType.HP => playerStats.getHP(),
			StatType.MP => playerStats.getMP(),
			StatType.Stamina => playerStats.getStamina(),
			_ => 0f
		};

		string currentStr = FormatValue(current, isPercent: false);
		string baseStr = FormatValue(baseVal, isPercent: false);
		string modStr = FormatModifierSuffix(mod, isPercent: false);
		return $"{currentStr}/{baseStr}{modStr}";
	}

	string FormatAttributeLine(StatType type, float baseVal, float mod)
	{
		bool isPercent = PercentageStats.Contains(type);
		string baseStr = FormatValue(baseVal, isPercent);
		string modStr = FormatModifierSuffix(mod, isPercent);
		return $"{baseStr}{modStr}";
	}

	static string FormatModifierSuffix(float mod, bool isPercent)
	{
		if (Mathf.Approximately(mod, 0f))
			return string.Empty;

		string sign = mod > 0f ? "+" : "";
		return $" <color=#D4AF37>({sign}{FormatValue(mod, isPercent)})</color>";
	}

	static string FormatValue(float value, bool isPercent)
	{
		if (isPercent)
			return $"{Mathf.RoundToInt(value)}%";

		if (Mathf.Approximately(value, Mathf.Round(value)))
			return Mathf.RoundToInt(value).ToString();

		return value.ToString("0.#");
	}

	static string FormatStatName(StatType statType)
	{
		return statType switch
		{
			StatType.HP => "HP",
			StatType.MP => "MP",
			StatType.Stamina => "Stamina",
			StatType.HPRegen => "HP Regen",
			StatType.MPRegen => "MP Regen",
			StatType.StaminaRegen => "Stamina Regen",
			StatType.ATK => "Attack",
			StatType.MAG => "Magic",
			StatType.DEF => "Defense",
			StatType.MDEF => "Magic Defense",
			StatType.CritChance => "Crit Rate",
			StatType.CritDamage => "Crit Damage",
			StatType.MS => "Move Speed",
			StatType.AS => "Attack Speed",
			StatType.DodgeChance => "Dodge",
			StatType.BlockChance => "Block",
			StatType.CooldownReduction => "Cooldown Reduction",
			_ => statType.ToString()
		};
	}
	#endregion
}
