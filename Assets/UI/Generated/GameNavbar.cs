using System;
using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(100)]
public class GameNavbar : MonoBehaviour
{
	#region Fields
	private UIDocument uiDocument;
	private VisualElement root;
	private Button inventoryBtn;
	private Button questJournalBtn;
	private Button characterBtn;

	EventCallback<ClickEvent> inventoryClickCallback;
	EventCallback<ClickEvent> questJournalClickCallback;
	EventCallback<ClickEvent> characterClickCallback;
	#endregion

	#region Lifecycle
	void Awake()
	{
		uiDocument = GetComponent<UIDocument>();
	}

	void OnEnable()
	{
		if (uiDocument == null)
			uiDocument = GetComponent<UIDocument>();

		UnbindButtons();
		root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogWarning("[GameNavbar] rootVisualElement null, deferring bind.", this);
			uiDocument?.rootVisualElement?.schedule.Execute(BindButtons).ExecuteLater(0);
			return;
		}

		root.schedule.Execute(BindButtons).ExecuteLater(0);
	}

	void OnDisable()
	{
		UnbindButtons();
	}
	#endregion

	#region Binding
	void BindButtons()
	{
		UnbindButtons();

		root = uiDocument != null ? uiDocument.rootVisualElement : null;
		inventoryBtn = root?.Q<Button>("inventory-btn");
		questJournalBtn = root?.Q<Button>("quests-btn");
		characterBtn = root?.Q<Button>("character-btn");

		if (inventoryBtn == null)
			Debug.LogWarning("[GameNavbar] inventory-btn not found in UXML.", this);
		else
			BindButton(inventoryBtn, OnInventoryBtnClicked, ref inventoryClickCallback);

		if (questJournalBtn == null)
			Debug.LogWarning("[GameNavbar] quests-btn not found in UXML.", this);
		else
			BindButton(questJournalBtn, OnQuestJournalBtnClicked, ref questJournalClickCallback);

		if (characterBtn == null)
			Debug.LogWarning("[GameNavbar] character-btn not found in UXML.", this);
		else
			BindButton(characterBtn, OnCharacterBtnClicked, ref characterClickCallback);
	}

	static void BindButton(Button button, Action handler, ref EventCallback<ClickEvent> clickCallback)
	{
		SetChildrenPickingIgnore(button);
		clickCallback = _ => handler();
		button.RegisterCallback(clickCallback);
	}

	static void SetChildrenPickingIgnore(VisualElement element)
	{
		foreach (VisualElement child in element.Children())
		{
			child.pickingMode = PickingMode.Ignore;
			SetChildrenPickingIgnore(child);
		}
	}

	void UnbindButtons()
	{
		UnbindButton(ref inventoryBtn, ref inventoryClickCallback);
		UnbindButton(ref questJournalBtn, ref questJournalClickCallback);
		UnbindButton(ref characterBtn, ref characterClickCallback);
	}

	static void UnbindButton(ref Button button, ref EventCallback<ClickEvent> clickCallback)
	{
		if (button == null)
			return;

		if (clickCallback != null)
		{
			button.UnregisterCallback(clickCallback);
			clickCallback = null;
		}

		button = null;
	}
	#endregion

	#region Handlers
	private void OnInventoryBtnClicked()
	{
		UIManager.UnlockCursorForUi();
		UIManager.Instance?.ToggleInventory();
	}

	private void OnQuestJournalBtnClicked()
	{
		UIManager.UnlockCursorForUi();
		if (UIManager.Instance == null)
		{
			Debug.LogError("[GameNavbar] UIManager.Instance is null.", this);
			return;
		}

		UIManager.Instance.ToggleQuestJournal();
	}

	private void OnCharacterBtnClicked()
	{
		UIManager.UnlockCursorForUi();
		UIManager.Instance?.ToggleCharacterScreen();
	}
	#endregion
}
