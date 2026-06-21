using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(100)]
public class InventoryPanel : MonoBehaviour
{
	#region Fields
	private UIDocument uiDocument;
	private VisualElement root;
	private ScrollView itemGridScroll;
	private Label detailName;
	private Label detailType;
	private Label detailDescription;
	private VisualElement statPreview;
	private Button closeBtn;
	private Button equipBtn;
	private Button useBtn;

	EventCallback<ClickEvent> closeClickCallback;
	EventCallback<ClickEvent> equipClickCallback;
	EventCallback<ClickEvent> useClickCallback;

	ItemSO selectedItem;
	InventoryComponent inventory;
	EquipmentComponent equipment;

	static readonly HashSet<StatType> PercentageStats = new()
	{
		StatType.CritChance,
		StatType.CritDamage,
		StatType.MS,
		StatType.AS,
		StatType.DodgeChance,
		StatType.BlockChance,
		StatType.CooldownReduction
	};
	#endregion

	#region Lifecycle
	void Awake()
	{
		uiDocument = GetComponent<UIDocument>();
	}

	void OnEnable()
	{
		UnbindButtons();
		EventBus.Subscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
		EventBus.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);

		root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogWarning("[InventoryPanel] root null, deferring bind.", this);
			uiDocument?.rootVisualElement?.schedule.Execute(BindUi).ExecuteLater(0);
			return;
		}

		root.schedule.Execute(BindUi).ExecuteLater(0);
	}

	void OnDisable()
	{
		EventBus.Unsubscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
		EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
		UnbindButtons();
	}
	#endregion

	#region Binding
	void BindUi()
	{
		UnbindButtons();

		if (uiDocument == null)
			uiDocument = GetComponent<UIDocument>();

		root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogWarning("[InventoryPanel] root still null.", this);
			return;
		}

		itemGridScroll = root.Q<ScrollView>(className: "item-grid");
		detailName = root.Q<Label>("detail-name");
		detailType = root.Q<Label>("detail-type");
		detailDescription = root.Q<Label>("detail-description");
		statPreview = root.Q<VisualElement>("stat-preview");
		closeBtn = root.Q<Button>("close-btn");
		equipBtn = root.Q<Button>("equip-btn");
		useBtn = root.Q<Button>("use-btn");

		if (closeBtn == null)
			Debug.LogWarning("[InventoryPanel] close-btn not found in UXML.", this);
		else
			BindButton(closeBtn, OnCloseBtnClicked, ref closeClickCallback);

		if (equipBtn == null)
			Debug.LogWarning("[InventoryPanel] equip-btn not found in UXML.", this);
		else
			BindButton(equipBtn, OnEquipBtnClicked, ref equipClickCallback);

		if (useBtn == null)
			Debug.LogWarning("[InventoryPanel] use-btn not found in UXML.", this);
		else
			BindButton(useBtn, OnUseBtnClicked, ref useClickCallback);

		ResolvePlayerComponents();
		Refresh();
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
		UnbindButton(ref closeBtn, ref closeClickCallback);
		UnbindButton(ref equipBtn, ref equipClickCallback);
		UnbindButton(ref useBtn, ref useClickCallback);
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

	void ResolvePlayerComponents()
	{
		inventory = FindFirstObjectByType<InventoryComponent>();
		equipment = FindFirstObjectByType<EquipmentComponent>();
	}

	void OnInventoryUpdated(InventoryUpdatedEvent e)
	{
		if (inventory != null && e.inventory != inventory)
			return;

		if (isActiveAndEnabled)
			Refresh();
	}

	void OnEquipmentChanged(EquipmentChangedEvent e)
	{
		if (equipment == null)
			ResolvePlayerComponents();

		if (equipment != null && e.equipment != equipment)
			return;

		if (isActiveAndEnabled)
			Refresh();
	}
	#endregion

	#region Handlers
	private void OnCloseBtnClicked()
	{
		if (UIManager.Instance != null)
			UIManager.Instance.CloseAll();
		else
		{
			gameObject.SetActive(false);
			UIManager.LockCursorForGameplay();
		}
	}

	private void OnEquipBtnClicked()
	{
		if (selectedItem is not EquipmentSO gear)
			return;

		if (equipment == null)
			ResolvePlayerComponents();

		if (equipment == null)
		{
			Debug.LogWarning("[InventoryPanel] EquipmentComponent not found in scene.", this);
			return;
		}

		if (equipment.IsEquipped(gear))
			equipment.Unequip(gear.slotType);
		else
			equipment.Equip(gear);

		Refresh();
	}

	private void OnUseBtnClicked()
	{
		if (selectedItem == null || selectedItem.itemType != ItemType.Consumable)
			return;

		if (inventory == null)
			ResolvePlayerComponents();

		if (inventory == null)
		{
			Debug.LogWarning("[InventoryPanel] InventoryComponent not found in scene.", this);
			return;
		}

		if (!inventory.TryUseItem(selectedItem))
			return;

		selectedItem = null;
		Refresh();
	}
	#endregion

	#region Data
	public void Refresh()
	{
		if (root == null || itemGridScroll == null)
		{
			BindUi();
			if (itemGridScroll == null)
				return;
		}

		ResolvePlayerComponents();
		RebuildItemGrid();

		if (inventory == null || inventory.Items.Count == 0)
		{
			selectedItem = null;
			ShowEmptyDetail("Inventory is empty.");
			return;
		}

		bool selectionStillValid = selectedItem != null && ItemListContains(inventory.Items, selectedItem);
		if (!selectionStillValid)
			selectedItem = inventory.Items[0];

		SelectItem(selectedItem);
	}

	void RebuildItemGrid()
	{
		if (itemGridScroll == null)
			return;

		itemGridScroll.contentContainer.Clear();

		if (inventory == null || inventory.Items.Count == 0)
		{
			var hint = new Label("No items yet. Pick up loot in the world.");
			hint.AddToClassList("empty-hint");
			itemGridScroll.contentContainer.Add(hint);
			return;
		}

		IReadOnlyList<ItemSO> items = inventory.Items;
		for (int i = 0; i < items.Count; i++)
		{
			ItemSO item = items[i];
			if (item == null)
				continue;

			VisualElement slot = CreateItemSlot(item);
			itemGridScroll.contentContainer.Add(slot);
		}
	}

	VisualElement CreateItemSlot(ItemSO item)
	{
		var slot = new VisualElement();
		slot.AddToClassList("item-slot");
		slot.userData = item;

		var icon = new VisualElement();
		icon.AddToClassList("slot-icon");
		if (item.icon != null)
			icon.style.backgroundImage = new StyleBackground(item.icon);

		var nameLabel = new Label(string.IsNullOrEmpty(item.itemName) ? "Item" : item.itemName);
		nameLabel.AddToClassList("slot-name");

		slot.Add(icon);
		slot.Add(nameLabel);
		slot.RegisterCallback<ClickEvent>(_ => SelectItem(item));
		return slot;
	}

	void SelectItem(ItemSO item)
	{
		selectedItem = item;
		UpdateListSelectionHighlight();
		ShowItemDetail(item);
	}

	void UpdateListSelectionHighlight()
	{
		if (itemGridScroll == null)
			return;

		foreach (VisualElement child in itemGridScroll.contentContainer.Children())
		{
			ItemSO item = child.userData as ItemSO;
			bool selected = item == selectedItem;
			bool equipped = item is EquipmentSO gear && equipment != null && equipment.IsEquipped(gear);
			child.EnableInClassList("item-slot--selected", selected);
			child.EnableInClassList("item-slot--equipped", equipped);
		}
	}

	void ShowEmptyDetail(string message)
	{
		if (detailName != null)
			detailName.text = "Select an item";
		if (detailType != null)
			detailType.text = "";
		if (detailDescription != null)
			detailDescription.text = message ?? "";
		RebuildStatPreview(null);
		UpdateActionButtons(null);
	}

	void ShowItemDetail(ItemSO item)
	{
		if (item == null)
		{
			ShowEmptyDetail("");
			return;
		}

		if (detailName != null)
			detailName.text = string.IsNullOrEmpty(item.itemName) ? "Unknown item" : item.itemName;
		if (detailType != null)
			detailType.text = GetItemTypeLabel(item);
		if (detailDescription != null)
			detailDescription.text = string.IsNullOrEmpty(item.description)
				? "No description."
				: item.description;

		RebuildStatPreview(item);
		UpdateActionButtons(item);
	}

	static string GetItemTypeLabel(ItemSO item)
	{
		if (item is EquipmentSO equipment)
			return $"{equipment.slotType} · Equipment";

		return item.itemType switch
		{
			ItemType.Consumable => "Consumable",
			ItemType.Quest => "Quest item",
			_ => item.itemType.ToString()
		};
	}

	void RebuildStatPreview(ItemSO item)
	{
		if (statPreview == null)
			return;

		statPreview.Clear();

		if (item == null)
			return;

		if (item is EquipmentSO equipment)
		{
			for (int i = 0; i < equipment.statModifiers.Count; i++)
			{
				EquipmentSO.StatModifier mod = equipment.statModifiers[i];
				AddStatRow(FormatStatName(mod.statType), FormatStatValue(mod.statType, mod.value));
			}

			if (equipment.statModifiers.Count == 0)
				AddStatRow("Stats", "—");
			return;
		}

		if (item.itemType == ItemType.Consumable && item.useAmount != 0f)
		{
			AddStatRow("Effect", FormatStatValue(item.useStatType, item.useAmount));
		}
	}

	void AddStatRow(string name, string value)
	{
		var row = new VisualElement();
		row.AddToClassList("stat-row");

		var nameLabel = new Label(name);
		nameLabel.AddToClassList("stat-name");

		var valueLabel = new Label(value);
		valueLabel.AddToClassList("stat-value");

		row.Add(nameLabel);
		row.Add(valueLabel);
		statPreview.Add(row);
	}

	void UpdateActionButtons(ItemSO item)
	{
		bool showEquip = item is EquipmentSO;
		bool showUse = item != null
			&& item.itemType == ItemType.Consumable
			&& item.useAmount != 0f;

		if (equipBtn != null)
		{
			equipBtn.style.display = showEquip ? DisplayStyle.Flex : DisplayStyle.None;
			if (item is EquipmentSO gear)
				equipBtn.text = equipment != null && equipment.IsEquipped(gear) ? "Unequip" : "Equip";
		}

		if (useBtn != null)
			useBtn.style.display = showUse ? DisplayStyle.Flex : DisplayStyle.None;
	}

	static string FormatStatName(StatType statType)
	{
		return statType switch
		{
			StatType.HP => "HP",
			StatType.MP => "MP",
			StatType.ATK => "Attack",
			StatType.MAG => "Magic",
			StatType.DEF => "Defense",
			StatType.MDEF => "Magic Defense",
			StatType.CritChance => "Crit Rate",
			StatType.CritDamage => "Crit Damage",
			StatType.MS => "Move Speed",
			StatType.AS => "Attack Speed",
			_ => statType.ToString()
		};
	}

	static string FormatStatValue(StatType statType, float value)
	{
		string sign = value >= 0f ? "+" : "";
		if (PercentageStats.Contains(statType))
			return $"{sign}{value}%";

		return $"{sign}{value}";
	}

	static bool ItemListContains(IReadOnlyList<ItemSO> items, ItemSO item)
	{
		if (items == null || item == null)
			return false;

		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] == item)
				return true;
		}

		return false;
	}
	#endregion
}
