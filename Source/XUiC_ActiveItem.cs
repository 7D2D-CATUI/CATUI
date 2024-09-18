using System;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class XUiC_ActiveItem : XUiController
{
	[PublicizedFrom(EAccessModifier.Protected)]
	public bool isDirty = true;

	[PublicizedFrom(EAccessModifier.Private)]
	public DateTime updateTime;

	[PublicizedFrom(EAccessModifier.Private)]
	public float deltaTime;

	[PublicizedFrom(EAccessModifier.Private)]
	public bool wasCrouching;

	[PublicizedFrom(EAccessModifier.Private)]
	public int currentSlotIndex = -1;

	[PublicizedFrom(EAccessModifier.Private)]
	public ItemClass itemClass;

	[PublicizedFrom(EAccessModifier.Private)]
	public XUiC_ActiveItemPartGrid grid;

	[field: PublicizedFrom(EAccessModifier.Private)]
	public EntityPlayerLocal LocalPlayer
	{
		get; [PublicizedFrom(EAccessModifier.Internal)]
		set;
	}

	public override void Init()
	{
		base.Init();
		grid = GetChildByType<XUiC_ActiveItemPartGrid>();
		IsDirty = true;
	}

	public override void Update(float _dt)
	{
		base.Update(_dt);
		deltaTime = _dt;
		if (LocalPlayer == null && XUi.IsGameRunning())
		{
			LocalPlayer = base.xui.playerUI.entityPlayer;
		}

		if (currentSlotIndex != base.xui.PlayerInventory.Toolbelt.GetFocusedItemIdx())
		{
			currentSlotIndex = base.xui.PlayerInventory.Toolbelt.GetFocusedItemIdx();
			IsDirty = true;
		}
		if (HasChanged() || IsDirty)
		{
			SetupActiveItemEntry();
			RefreshBindings(_forceAll: true);
			IsDirty = false;
		}
	}

	public bool HasChanged()
	{
		bool result = false;
		ItemAction itemAction = LocalPlayer.inventory.holdingItemItemValue.ItemClass.Actions[0];
		if (itemAction != null && itemAction.IsEditingTool())
		{
			result = itemAction.IsStatChanged();
		}
		return result;
	}

	[PublicizedFrom(EAccessModifier.Private)]
	public void SetupActiveItemEntry()
	{
		itemClass = null;
		EntityPlayer localPlayer = LocalPlayer;
		if (!localPlayer)
		{
			return;
		}
		Inventory inventory = localPlayer.inventory;
		ItemStack itemStack = inventory.GetItem(currentSlotIndex);
		if (itemStack != null && itemStack.itemValue.Modifications.Length > 0)
		{
			Log.Warning("XUiC_ActiveItem itemStack.itemValue.Modifications.Length: " + itemStack.itemValue.Modifications.Length);

			//grid.CurrentItem = itemStack;
			//grid.SetParts(itemStack.itemValue.Modifications);
			grid.ViewComponent.IsVisible = true;
		}
		else {
			grid.ViewComponent.IsVisible = false;
		}
		itemClass = itemStack.itemValue.ItemClass;
	}

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "test":
				value = "test XUiC_ActiveItem";
                return true;
            default:
                return false;
        }
    }

    public override void OnOpen()
	{
		base.OnOpen();
		base.xui.PlayerInventory.OnBackpackItemsChanged += PlayerInventory_OnBackpackItemsChanged;
		base.xui.PlayerInventory.OnToolbeltItemsChanged += PlayerInventory_OnToolbeltItemsChanged;
		IsDirty = true;
		RefreshBindings(_forceAll: true);
	}

	public override void OnClose()
	{
		base.OnClose();
		base.xui.PlayerInventory.OnBackpackItemsChanged -= PlayerInventory_OnBackpackItemsChanged;
		base.xui.PlayerInventory.OnToolbeltItemsChanged -= PlayerInventory_OnToolbeltItemsChanged;
	}

	[PublicizedFrom(EAccessModifier.Private)]
	public void PlayerInventory_OnToolbeltItemsChanged()
	{
		IsDirty = true;
	}

	[PublicizedFrom(EAccessModifier.Private)]
	public void PlayerInventory_OnBackpackItemsChanged()
	{
		IsDirty = true;
	}
}
