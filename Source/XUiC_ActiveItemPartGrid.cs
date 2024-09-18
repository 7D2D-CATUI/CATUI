using System;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections.Generic;
[Preserve]
public class XUiC_ActiveItemPartGrid : XUiController
{
	[PublicizedFrom(EAccessModifier.Protected)]
	public int curPageIdx;

	[PublicizedFrom(EAccessModifier.Protected)]
	public int numPages;

	[PublicizedFrom(EAccessModifier.Protected)]
	public XUiController[] itemControllers;

	[PublicizedFrom(EAccessModifier.Private)]
	public List<XUiC_ActiveItemPartEntry> entryList = new List<XUiC_ActiveItemPartEntry>();

	[PublicizedFrom(EAccessModifier.Protected)]
	public ItemStack[] items;

	[PublicizedFrom(EAccessModifier.Private)]
	public ItemClass currentItemClass;

	[field: PublicizedFrom(EAccessModifier.Private)]
	public ItemStack CurrentItem
	{
		get; set;
	}

	public override void Init()
	{
		base.Init();
		//XUiController[] childrenByType = GetChildrenByType<XUiC_ActiveItemPartEntry>();
		//itemControllers = childrenByType;
		IsDirty = false;
	}

	public override void Update(float _dt)
	{
		if (!(GameManager.Instance == null) || GameManager.Instance.World != null)
		{
			base.Update(_dt);
		}
	}

	public void SetParts(ItemValue[] stackList)
	{
		if (stackList == null)
		{
			return;
		}
		for (int i = 0; i < stackList.Length; i++)
		{
			// XUiC_ActiveItemPartEntry _XUiC_ActiveItemPartEntry = (XUiC_ActiveItemPartEntry)itemControllers[i];

			Log.Warning("XUiC_ActiveItemPartGrid list index: " + i);
			Log.Warning("XUiC_ActiveItemPartGrid list ID: " + stackList[i]?.ItemClass?.GetItemName());


			// ItemValue itemValue = stackList[i];
			// xUiC_ItemPartStack.ItemValue = itemValue != null ? itemValue : ItemValue.None.Clone();
			// xUiC_ItemPartStack.PartNumber = i;
		}
		//currentItemClass = CurrentItem.itemValue.ItemClass;
		//ItemValue[] Modifications = CurrentItem.itemValue.Modifications;
		//for (int i = 0; i < Modifications.Length; i++)
		//{
		//	XUiC_ActiveItemPartEntry xUiC_ItemPartStack = (XUiC_ActiveItemPartEntry)itemControllers[i];
		//	ItemValue itemValue = CurrentItem.itemValue.Modifications[i];
		//	xUiC_ItemPartStack.ItemValue = itemValue != null ? itemValue : ItemValue.None.Clone();
		//	xUiC_ItemPartStack.PartNumber = i;
		//}
	}
}
