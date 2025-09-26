using System.Runtime.CompilerServices;
using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STMG.Engine;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(UpgradeUI))]
public static class UpgradeUI_Patches
{
    [HarmonyPatch(typeof(UpgradeUI), MethodType.Constructor, [typeof(VehicleBaseUser), typeof(GameScene), typeof(Action)]), HarmonyPrefix]
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public static bool UpgradeUI_UpgradeUI_Prefix(UpgradeUI __instance, VehicleBaseUser vehicle, GameScene scene, Action after = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    {
        ExtensionsHelper.SetPrivateField(__instance, "scene", scene);
        ExtensionsHelper.SetPrivateField(__instance, "vehicle", vehicle);
        ExtensionsHelper.SetPrivateField(__instance, "after", after);
        TooltipPreset tooltip = TooltipPreset.Get(" <!cicon_upgrade> " + Localization.GetGeneral("replace"), scene.Engine, can_lock: true);
        ExtensionsHelper.SetPrivateField(__instance, "tooltip", tooltip);
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetOptions", []);
        if (ExtensionsHelper.GetPrivateField<VehicleBaseEntity>(__instance, "option") == null)
        {
            MainData.Sound_error.Play();
            return false;
        }

        // up button
        Button buttonUp = ButtonPresets.TextBlack(
            new ContentRectangle(0f, 0f, 0f, MainData.Size_button/2, 1f),
            "<!cicon_up>",
            scene.Engine).Control;
        buttonUp.horizontal_alignment = HorizontalAlignment.Stretch;
        buttonUp.OnButtonPress += new Action(() => __instance.UpgradeUI_VehicleNext_Ext());
        tooltip.AddContent(buttonUp);

        // dropdown list
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetVehicles", []);

        // down button
        Button buttonDown = ButtonPresets.TextBlack(
            new ContentRectangle(0f, 0f, 0f, MainData.Size_button/2, 1f),
            "<!cicon_down>",
            scene.Engine).Control;
        buttonDown.horizontal_alignment = HorizontalAlignment.Stretch;
        buttonDown.OnButtonPress += new Action(() => __instance.UpgradeUI_VehiclePrev_Ext());
        tooltip.AddContent(buttonDown);

        ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetIcons", []);
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetStats", []);
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetControls", []);
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetPrice", []);
        tooltip.Main_control.OnUpdate += new Action(() => UpgradeUI_Update_Reverse(__instance));

        return false; // skip the original
    }


    // Extension method to find out the next in chain vehicle model
    public static void UpgradeUI_VehicleNext_Ext(this UpgradeUI ui)
    {
        //Log.Write($"button clicked");
        VehicleBaseUser vehicle = ExtensionsHelper.GetPrivateField<VehicleBaseUser>(ui, "vehicle");
        //Log.Write($"vehicle is {vehicle.Entity_base.Translated_name} from {vehicle.Entity_base.Company.Entity.Translated_name}");

        VehicleBaseEntity[] filtered = ExtensionsHelper.GetPrivateField<GrowArray<VehicleBaseEntity>>(ui, "options").ToArray()
                           .Where(option => (option.Company.Entity.ID == vehicle.Entity_base.Company.Entity.ID) && (option.Price > vehicle.Entity_base.Price))
                           .OrderBy(option => option.Price).ToArray();

        //foreach (VehicleBaseEntity item in filtered)
        //Log.Write($"{item.Tier} {item.Translated_name} from {item.Company.Entity.Translated_name} price: {item.Price}");

        if (filtered.Length > 0)
            ExtensionsHelper.CallPrivateMethodVoid(ui, "SelectOption", [filtered[0]]);
        else
            MainData.Sound_error.Play();
    }


    // Extension method to find out the prev in chain vehicle model
    public static void UpgradeUI_VehiclePrev_Ext(this UpgradeUI ui)
    {
        //Log.Write($"button clicked");
        VehicleBaseUser vehicle = ExtensionsHelper.GetPrivateField<VehicleBaseUser>(ui, "vehicle");
        //Log.Write($"vehicle is {vehicle.Entity_base.Translated_name} from {vehicle.Entity_base.Company.Entity.Translated_name}");

        VehicleBaseEntity[] filtered = ExtensionsHelper.GetPrivateField<GrowArray<VehicleBaseEntity>>(ui, "options").ToArray()
                           .Where(option => (option.Company.Entity.ID == vehicle.Entity_base.Company.Entity.ID) && (option.Price < vehicle.Entity_base.Price))
                           .OrderByDescending(option => option.Price).ToArray();

        //foreach (VehicleBaseEntity item in filtered)
        //Log.Write($"{item.Tier} {item.Translated_name} from {item.Company.Entity.Translated_name} price: {item.Price}");

        if (filtered.Length > 0)
            ExtensionsHelper.CallPrivateMethodVoid(ui, "SelectOption", [filtered[0]]);
        else
            MainData.Sound_error.Play();
    }


    [HarmonyPatch("Update"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpgradeUI_Update_Reverse(UpgradeUI __instance) =>
        throw new NotImplementedException("ERROR. UpgradeUI_Update_Reverse");


    // this method fills up available vehicles; all fields are private in this class
    [HarmonyPatch("GetVehiclesOptions"), HarmonyPrefix]
    public static bool UpgradeUI_GetVehiclesOptions_Prefix(
        UpgradeUI __instance, ref DropdownSettings __result,
        GrowArray<VehicleBaseEntity> ___options, // private field
        Grid grid)
    {
        //Log.Write($"grid {grid.Columns_count}x{grid.Rows_count} n={grid.Items_count}"); // debug
        GrowArray<SimpleDropdownItem> _items = new GrowArray<SimpleDropdownItem>();
        for (int i = 0; i < ___options.Count; i++)
        {
            // MODDED: add Capacity Min/Max, Company, Range for Plane
            VehicleBaseEntity vbe = ___options[i];
            int maxCapacity = vbe is TrainEntity train ? train.Max_capacity : vbe.Capacity;
            // format: company tier icon name min/max speed [range]
            string vehicle = $"{vbe.Company.Entity.Translated_name[..2]}. {___options[i].GetNameWithIcons()}  {vbe.Real_min_passengers}/{maxCapacity}<!cicon_passenger>  {vbe.Speed}<!cicon_fast>";
            if (vbe is PlaneEntity plane) vehicle += $"  {plane.Range}<!cicon_locate>";
            //vehicle += "<!cicon_fast> <!cicon_pin> <!cicon_right> <!cicon_select> <!cicon_locate>"; // TEST ICONS
            _items.Add(new SimpleDropdownItem(i, vehicle));
        }
        // magic here, to pass Func<int> we must go through lambda, same for Action<>
        __result = new DropdownSettings(get_current: () => UpgradeUI_GetCurrent_Reverse(__instance), items: _items, on_select: (item) => UpgradeUI_OnVehicleSelect_Reverse(__instance, item));
        __result.content = grid;
        ExtensionsHelper.SetPrivateField(__instance, "dropdown", __result);

        return false; // skip original
    }
    
    [HarmonyPatch("GetCurrent"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int UpgradeUI_GetCurrent_Reverse(UpgradeUI __instance) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. UpgradeUI_GetCurrent_Reverse");
    
    [HarmonyPatch("OnVehicleSelect", [typeof(SimpleDropdownItem )]), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpgradeUI_OnVehicleSelect_Reverse(UpgradeUI __instance, SimpleDropdownItem item) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. UpgradeUI_OnVehicleSelect_Reverse");
}
