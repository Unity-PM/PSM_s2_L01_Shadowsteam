using UnityEngine;
using UnityEngine.UIElements;

public class GameNavbar : MonoBehaviour
{
	#region Fields
	private UIDocument uiDocument;
	private VisualElement root;
	private Button inventoryBtn;
	private Button questJournalBtn;
	private Button characterBtn;
	private Button closeBtn;
	#endregion

	#region Lifecycle
	public void OnEnable()
	{
		uiDocument = GetComponent<UIDocument>();
		root = uiDocument.rootVisualElement;
		inventoryBtn = root.Q<Button>("inventory-btn");
		questJournalBtn = root.Q<Button>("quest-journal-btn");
		characterBtn = root.Q<Button>("character-btn");
		closeBtn = root.Q<Button>("close-btn");
		inventoryBtn.clicked += OnInventoryBtnClicked;
		questJournalBtn.clicked += OnQuestJournalBtnClicked;
		characterBtn.clicked += OnCharacterBtnClicked;
		closeBtn.clicked += OnCloseBtnClicked;
	}

	public void OnDisable()
	{
		root.Clear();
	}
	#endregion

	#region Handlers
	private void OnInventoryBtnClicked()
	{
		// TODO: Handle inventory button click
	}

	private void OnQuestJournalBtnClicked()
	{
		// TODO: Handle quest journal button click
	}

	private void OnCharacterBtnClicked()
	{
		// TODO: Handle character button click
	}

	private void OnCloseBtnClicked()
	{
		// TODO: Handle close button click
	}
	#endregion

	#region Data
	public void Refresh()
	{
		// TODO: Refresh data
	}
	#endregion
}