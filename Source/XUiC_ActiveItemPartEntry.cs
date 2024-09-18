using InControl;
using UnityEngine;
using UnityEngine.Scripting;

[UnityEngine.Scripting.Preserve]
public class XUiC_ActiveItemPartEntry : XUiController
{
    [PublicizedFrom(EAccessModifier.Protected)]
    public bool isDirty = true;

    //[PublicizedFrom(EAccessModifier.Private)]
    //public ItemValue itemValue;

    //[PublicizedFrom(EAccessModifier.Private)]
    //public int partNumber;

    //public ItemValue ItemValue
    //{
    //    get
    //    {
    //        return itemValue;
    //    }
    //    set
    //    {
    //        itemValue = value;
    //    }
    //}

    //public int PartNumber
    //{
    //    get
    //    {
    //        return partNumber;
    //    }
    //    set
    //    {
    //        partNumber = value;
    //    }
    //}

    public override void Init()
    {
        base.Init();

        Log.Warning("XUiC_ActiveItemPartEntry Init: ");
        // Log.Warning("XUiC_ActiveItemPartEntry PartNumber: " + PartNumber);
    }

    //public override void Update(float _dt)
    //{
    //    base.Update(_dt);
    //    if (isDirty)
    //    {
    //        // RefreshBindings(_forceAll: true);
    //        isDirty = false;
    //    }
    //}

    //public override bool GetBindingValue(ref string value, string bindingName)
    //{
    //    switch (bindingName)
    //    {
    //        case "CATUI_PartIcon":
    //            value = "";
    //            if (ItemValue != null)
    //            {
    //                value = ItemValue.ItemClass.GetIconName();
    //                return true;
    //            }
    //            return true;
    //        case "CATUI_PartNumber":
    //            value = "-1";
    //            if (ItemValue != null)
    //            {
    //                value = PartNumber.ToString();
    //                return true;
    //            }
    //            return true;
    //        default:
    //            return false;
    //    }
    //}

    //[PublicizedFrom(EAccessModifier.Protected)]
    //public ItemStack itemStack = ItemStack.Empty.Clone();

    //[PublicizedFrom(EAccessModifier.Protected)]
    //public ItemClass itemClass;

    //[PublicizedFrom(EAccessModifier.Protected)]
    //public string slotType;

    //[PublicizedFrom(EAccessModifier.Protected)]
    //public XUiController stackValue;

    //[PublicizedFrom(EAccessModifier.Private)]
    //public readonly CachedStringFormatterInt qualityFormatter = new CachedStringFormatterInt();

    //[PublicizedFrom(EAccessModifier.Private)]
    //public readonly CachedStringFormatterXuiRgbaColor partcolorFormatter = new CachedStringFormatterXuiRgbaColor();

    //[PublicizedFrom(EAccessModifier.Private)]
    //public readonly CachedStringFormatterFloat partfillFormatter = new CachedStringFormatterFloat();

    //[PublicizedFrom(EAccessModifier.Protected)]
    //public string emptySpriteName = "";

    //[field: PublicizedFrom(EAccessModifier.Private)]
    //public int SlotNumber
    //{
    //    get; set;
    //}

    //[field: PublicizedFrom(EAccessModifier.Private)]
    //public XUiC_ItemStack.StackLocationTypes StackLocation
    //{
    //    get; set;
    //}

    //public string SlotType
    //{
    //    get
    //    {
    //        return slotType;
    //    }
    //    set
    //    {
    //        slotType = value;
    //        SetEmptySpriteName();
    //    }
    //}

    //public ItemStack ItemStack
    //{
    //    get
    //    {
    //        return itemStack;
    //    }
    //    set
    //    {
    //        if (itemStack != value)
    //        {
    //            itemStack = value;
    //            isDirty = true;
    //            itemClass = itemStack.itemValue.ItemClass;
    //            RefreshBindings();
    //        }
    //    }
    //}

    //public ItemClass ItemClass => itemClass;

    //public virtual string GetAtlas()
    //{
    //    if (itemClass == null)
    //    {
    //        return "ItemIconAtlasGreyscale";
    //    }
    //    return "ItemIconAtlas";
    //}

    //public virtual string GetPartName()
    //{
    //    if (itemClass == null)
    //    {
    //        return $"[MISSING {SlotType}]";
    //    }
    //    return itemClass.GetLocalizedItemName();
    //}

    //public override bool GetBindingValue(ref string value, string bindingName)
    //{
    //    switch (bindingName)
    //    {
    //        case "partname":
    //            value = GetPartName();
    //            return true;
    //        case "partquality":
    //            value = ((itemClass != null && itemStack != null) ? qualityFormatter.Format(itemStack.itemValue.Quality) : "");
    //            return true;
    //        case "partatlas":
    //            value = GetAtlas();
    //            return true;
    //        case "particon":
    //            if (itemClass == null)
    //            {
    //                value = emptySpriteName;
    //            }
    //            else
    //            {
    //                value = itemStack.itemValue.GetPropertyOverride("CustomIcon", (itemClass.CustomIcon != null) ? itemClass.CustomIcon.Value : itemClass.GetIconName());
    //            }
    //            return true;
    //        case "particoncolor":
    //            if (itemClass == null)
    //            {
    //                value = "255, 255, 255, 178";
    //            }
    //            else
    //            {
    //                Color32 color = itemStack.itemValue.ItemClass.GetIconTint(itemStack.itemValue);
    //                value = $"{color.r},{color.g},{color.b},{color.a}";
    //            }
    //            return true;
    //        case "partcolor":
    //            if (itemClass != null)
    //            {
    //                Color32 v = QualityInfo.GetQualityColor(itemStack.itemValue.Quality);
    //                value = partcolorFormatter.Format(v);
    //            }
    //            else
    //            {
    //                value = "255, 255, 255, 0";
    //            }
    //            return true;
    //        case "partvisible":
    //            value = ((itemClass != null) ? "true" : "false");
    //            return true;
    //        case "emptyvisible":
    //            value = ((itemClass == null) ? "true" : "false");
    //            return true;
    //        case "partfill":
    //            value = ((itemStack.itemValue.MaxUseTimes == 0) ? "1" : partfillFormatter.Format(((float)itemStack.itemValue.MaxUseTimes - itemStack.itemValue.UseTimes) / (float)itemStack.itemValue.MaxUseTimes));
    //            return true;
    //        default:
    //            return false;
    //    }
    //}

    //public override void Init()
    //{
    //    base.Init();
    //    RefreshBindings();
    //}

    //public override void Update(float _dt)
    //{
    //    base.Update(_dt);
    //}

    //[PublicizedFrom(EAccessModifier.Protected)]
    //public virtual void SetEmptySpriteName()
    //{
    //}

}
