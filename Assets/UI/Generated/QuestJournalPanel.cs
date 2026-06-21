using System.Collections.Generic;
using Platformer;
using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(100)]
public class QuestJournalPanel : MonoBehaviour
{
	#region Fields
	[SerializeField] private QuestManager questManager;

	private UIDocument uiDocument;
	private VisualElement rootVisualElement;
	private ScrollView questListScroll;
	private ScrollView objectivesListScroll;
	private Label detailTitle;
	private Label detailStatus;
	private Label detailCategory;
	private Label detailDescription;
	private Label detailReward;
	private Button closeBtn;

	readonly List<JournalQuestEntry> journalEntries = new();
	readonly List<string> completedIdBuffer = new();
	VisualElement selectedListItem;
	string selectedQuestId;
	QuestManager subscribedManager;
	#endregion

	#region Lifecycle
	void OnEnable()
	{
		TryBindUi();
		BindQuestManager();
		Refresh();
	}

	void OnDisable()
	{
		if (closeBtn != null)
			closeBtn.clicked -= OnCloseBtnClicked;

		UnbindQuestManager();
	}

	void TryBindUi()
	{
		if (questManager == null)
			questManager = FindFirstObjectByType<QuestManager>();

		uiDocument = GetComponent<UIDocument>();
		if (uiDocument == null)
		{
			Debug.LogError("[QuestJournalPanel] UIDocument missing.", this);
			return;
		}

		rootVisualElement = uiDocument.rootVisualElement;
		if (rootVisualElement == null)
		{
			Debug.LogWarning("[QuestJournalPanel] rootVisualElement null (UIDocument not ready?).", this);
			return;
		}

		questListScroll = rootVisualElement.Q<ScrollView>("quest-list");
		objectivesListScroll = rootVisualElement.Q<ScrollView>("objectives-list");
		detailTitle = rootVisualElement.Q<Label>("detail-title");
		detailStatus = rootVisualElement.Q<Label>("detail-status");
		detailCategory = rootVisualElement.Q<Label>("detail-category");
		detailDescription = rootVisualElement.Q<Label>("detail-description");
		detailReward = rootVisualElement.Q<Label>("detail-reward");
		closeBtn = rootVisualElement.Q<Button>("close-btn");

		if (closeBtn == null)
			Debug.LogWarning("[QuestJournalPanel] close-btn not found in UXML.", this);
		else
		{
			closeBtn.clicked -= OnCloseBtnClicked;
			closeBtn.clicked += OnCloseBtnClicked;
		}
	}

	void BindQuestManager()
	{
		if (questManager == null)
			questManager = FindFirstObjectByType<QuestManager>();

		if (subscribedManager == questManager)
			return;

		UnbindQuestManager();
		if (questManager == null)
			return;

		questManager.QuestJournalChanged += OnQuestJournalChanged;
		subscribedManager = questManager;
	}

	void UnbindQuestManager()
	{
		if (subscribedManager == null)
			return;

		subscribedManager.QuestJournalChanged -= OnQuestJournalChanged;
		subscribedManager = null;
	}

	void OnQuestJournalChanged() => Refresh();
	#endregion

	#region Handlers
	private void OnCloseBtnClicked()
	{
		if (UIManager.Instance != null)
			UIManager.Instance.CloseAll();
		else
		{
			Debug.LogWarning("[QuestJournalPanel] UIManager.Instance null, deactivating self.", this);
			gameObject.SetActive(false);
		}
	}
	#endregion

	#region Data
	public void Refresh()
	{
		if (questListScroll == null)
		{
			TryBindUi();
			if (questListScroll == null)
			{
				Debug.LogError("[QuestJournalPanel] quest-list ScrollView still null.", this);
				return;
			}
		}

		if (questManager == null)
		{
			Debug.LogWarning("[QuestJournalPanel] QuestManager not found.", this);
			return;
		}

		BuildJournalEntries();
		RebuildQuestList();

		if (journalEntries.Count == 0)
		{
			selectedQuestId = null;
			selectedListItem = null;
			ShowEmptyDetail();
			return;
		}

		bool selectionStillValid = false;
		foreach (JournalQuestEntry entry in journalEntries) {
			if (entry.QuestId == selectedQuestId) {
				selectionStillValid = true;
				break;
			}
		}

		if (!selectionStillValid)
			selectedQuestId = journalEntries[0].QuestId;

		SelectQuest(selectedQuestId);
	}

	void BuildJournalEntries()
	{
		journalEntries.Clear();
		completedIdBuffer.Clear();
		questManager.CopyCompletedQuestIds(completedIdBuffer);

		IReadOnlyList<QuestRuntimeState> active = questManager.ActiveQuests;
		for (int i = 0; i < active.Count; i++) {
			QuestRuntimeState state = active[i];
			if (state?.Definition == null)
				continue;

			journalEntries.Add(new JournalQuestEntry(state.Definition.QuestId, state.Definition, true, state));
		}

		for (int i = 0; i < completedIdBuffer.Count; i++) {
			string questId = completedIdBuffer[i];
			if (string.IsNullOrEmpty(questId) || IsQuestIdActive(questId))
				continue;

			if (!questManager.TryGetQuestDefinition(questId, out QuestDefinition definition))
				continue;

			journalEntries.Add(new JournalQuestEntry(questId, definition, false, null));
		}
	}

	bool IsQuestIdActive(string questId)
	{
		IReadOnlyList<QuestRuntimeState> active = questManager.ActiveQuests;
		for (int i = 0; i < active.Count; i++) {
			if (active[i]?.Definition?.QuestId == questId)
				return true;
		}

		return false;
	}

	void RebuildQuestList()
	{
		questListScroll.contentContainer.Clear();
		selectedListItem = null;

		if (journalEntries.Count == 0) {
			var hint = new Label("No active or completed quests yet.");
			hint.AddToClassList("empty-hint");
			questListScroll.contentContainer.Add(hint);
			return;
		}

		for (int i = 0; i < journalEntries.Count; i++) {
			JournalQuestEntry entry = journalEntries[i];
			VisualElement row = CreateQuestListItem(entry);
			questListScroll.contentContainer.Add(row);
		}
	}

	VisualElement CreateQuestListItem(JournalQuestEntry entry)
	{
		var row = new VisualElement();
		row.AddToClassList("quest-list-item");
		row.userData = entry.QuestId;

		var title = new Label(string.IsNullOrEmpty(entry.Definition.Title)
			? entry.QuestId
			: entry.Definition.Title);
		title.AddToClassList("quest-list-item__title");

		string statusText = entry.IsActive ? "In progress" : "Completed";
		string categoryText = entry.Definition.Category.ToString();
		var meta = new Label($"{categoryText} · {statusText}");
		meta.AddToClassList("quest-list-item__meta");

		row.Add(title);
		row.Add(meta);

		row.RegisterCallback<ClickEvent>(_ => SelectQuest(entry.QuestId));
		return row;
	}

	void SelectQuest(string questId)
	{
		selectedQuestId = questId;
		UpdateListSelectionHighlight();

		JournalQuestEntry entry = FindEntry(questId);
		if (entry == null) {
			ShowEmptyDetail();
			return;
		}

		ShowQuestDetail(entry);
	}

	JournalQuestEntry FindEntry(string questId)
	{
		for (int i = 0; i < journalEntries.Count; i++) {
			if (journalEntries[i].QuestId == questId)
				return journalEntries[i];
		}

		return null;
	}

	void UpdateListSelectionHighlight()
	{
		foreach (VisualElement child in questListScroll.contentContainer.Children()) {
			bool selected = child.userData as string == selectedQuestId;
			child.EnableInClassList("quest-list-item--selected", selected);
			if (selected)
				selectedListItem = child;
		}
	}

	void ShowEmptyDetail()
	{
		detailTitle.text = "Select a quest";
		detailStatus.text = "";
		detailStatus.RemoveFromClassList("quest-status--active");
		detailStatus.RemoveFromClassList("quest-status--completed");
		detailCategory.text = "";
		detailDescription.text = journalEntries.Count == 0
			? "Accept quests from NPCs or story triggers to see them here."
			: "";
		detailReward.text = "";
		objectivesListScroll.contentContainer.Clear();
	}

	void ShowQuestDetail(JournalQuestEntry entry)
	{
		QuestDefinition def = entry.Definition;
		detailTitle.text = string.IsNullOrEmpty(def.Title) ? def.QuestId : def.Title;

		detailStatus.RemoveFromClassList("quest-status--active");
		detailStatus.RemoveFromClassList("quest-status--completed");
		if (entry.IsActive) {
			detailStatus.text = "Status: In progress";
			detailStatus.AddToClassList("quest-status--active");
		} else {
			detailStatus.text = "Status: Completed";
			detailStatus.AddToClassList("quest-status--completed");
		}

		detailCategory.text = $"Category: {def.Category}";
		detailDescription.text = string.IsNullOrEmpty(def.Description)
			? "No description."
			: def.Description;

		var rewardParts = new List<string>();
		if (def.ExperienceReward > 0)
			rewardParts.Add($"{def.ExperienceReward} XP");
		if (!string.IsNullOrEmpty(def.NextQuestId))
			rewardParts.Add($"Next: {def.NextQuestId}");
		detailReward.text = rewardParts.Count > 0
			? $"Reward: {string.Join(" · ", rewardParts)}"
			: "Reward: —";

		RebuildObjectivesList(entry);
	}

	void RebuildObjectivesList(JournalQuestEntry entry)
	{
		objectivesListScroll.contentContainer.Clear();
		QuestDefinition def = entry.Definition;
		IReadOnlyList<QuestObjective> objectives = def.Objectives;

		for (int i = 0; i < objectives.Count; i++) {
			QuestObjective objective = objectives[i];
			if (objective == null)
				continue;

			int current = entry.IsActive && entry.ActiveState != null && i < entry.ActiveState.Progress.Length
				? entry.ActiveState.Progress[i]
				: objective.GetProgressCap();
			int cap = objective.GetProgressCap();
			bool done = current >= cap;

			var row = new VisualElement();
			row.AddToClassList("objective-row");

			var bullet = new Label(done ? "✓" : "•");
			bullet.AddToClassList("objective-row__bullet");

			var text = new Label(QuestJournalObjectiveText.Format(objective, current, cap));
			text.AddToClassList("objective-row__text");
			if (done)
				text.AddToClassList("objective-row__text--done");

			row.Add(bullet);
			row.Add(text);
			objectivesListScroll.contentContainer.Add(row);
		}

		if (objectives.Count == 0) {
			var hint = new Label("No objectives defined.");
			hint.AddToClassList("empty-hint");
			objectivesListScroll.contentContainer.Add(hint);
		}
	}
	#endregion

	sealed class JournalQuestEntry
	{
		public string QuestId { get; }
		public QuestDefinition Definition { get; }
		public bool IsActive { get; }
		public QuestRuntimeState ActiveState { get; }

		public JournalQuestEntry(string questId, QuestDefinition definition, bool isActive,
			QuestRuntimeState activeState)
		{
			QuestId = questId;
			Definition = definition;
			IsActive = isActive;
			ActiveState = activeState;
		}
	}
}

static class QuestJournalObjectiveText
{
	public static string Format(QuestObjective objective, int current, int cap)
	{
		switch (objective) {
			case KillObjective kill:
				return $"Defeat enemies ({kill.TargetTag}): {current}/{cap}";
			case CollectObjective collect:
				return $"Collect {collect.DisplayName}: {current}/{cap}";
			case BreakObjectObjective breakObject:
				return $"Break {breakObject.BreakableId}: {current}/{cap}";
			case TalkObjective talk:
				return current >= cap
					? $"Talk to {talk.DisplayName} (done)"
					: $"Talk to {talk.DisplayName}";
			case ReachLocationObjective reach:
				return current >= cap
					? $"Reach {reach.LocationId} (done)"
					: $"Reach {reach.LocationId}";
			default:
				return $"Objective: {current}/{cap}";
		}
	}
}
