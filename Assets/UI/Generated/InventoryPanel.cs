using UnityEngine;
using UnityEngine.UIElements;

public class InventoryPanel : MonoBehaviour
{
	#region Fields
	private UIDocument uiDocument;
	private VisualElement rootVisualElement;
	private Button closeBtn;
	private Button equipBtn;
	#endregion

	#region Lifecycle
	void OnEnable()
	{
		uiDocument = GetComponent<UIDocument>();
		rootVisualElement = uiDocument.rootVisualElement;
		closeBtn = rootVisualElement.Q<Button>("close-btn");
		equipBtn = rootVisualElement.Q<Button>("equip-btn");
		closeBtn.clicked += OnCloseBtnClicked;
		equipBtn.clicked += OnEquipBtnClicked;
	}

	void OnDisable()
	{
		rootVisualElement.Clear();
	}
	#endregion

	#region Handlers
	private void OnCloseBtnClicked()
	{
		// TODO: Handle close button click
	}

	private void OnEquipBtnClicked()
	{
		// TODO: Handle equip button click
	}
	#endregion

	#region Data
	public void Refresh()
	{
		// TODO: Update labels and values
	}
	#endregion
}