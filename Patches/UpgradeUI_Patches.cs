using System.Runtime.CompilerServices;
using HarmonyLib;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;

[HarmonyPatch(typeof(UpgradeUI))]
public static class UpgradeUI_Patches
{
    /* not used
    [HarmonyPatch("Get"), HarmonyPrefix]
    public static bool UpgradeUI_Get_Prefix(UpgradeUI __instance, IControl parent, VehicleBaseUser vehicle, GameScene scene)
    {
        Log.Write($"{vehicle.ID} {vehicle.Type} c={vehicle.Company} s={vehicle.Speed} t={vehicle.Throughput.GetLastMonth()} b={vehicle.Balance.GetLastMonth()}");
        //Log.WriteCallingStack(10);
        return true; // continue
    }
    */

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
            int maxCapacity = vbe is TrainEntity ? ((TrainEntity)vbe).Max_capacity : vbe.Capacity;
            // format: company tier icon name min/max speed [range]
            string vehicle = $"{vbe.Company.Entity.Translated_name.Substring(0,2)}. {___options[i].GetNameWithIcons()}  {vbe.Real_min_passengers}/{maxCapacity}<!cicon_passenger>  {vbe.Speed}<!cicon_fast>";
            if (vbe is PlaneEntity) vehicle += "  " + ((PlaneEntity)vbe).Range + "<!cicon_locate>";
            //vehicle += "<!cicon_fast> <!cicon_pin> <!cicon_right> <!cicon_select> <!cicon_locate>"; // TEST ICONS
            _items.Add(new SimpleDropdownItem(i, vehicle));
        }
        // magic here, to pass Func<int> we must go through lambda, same for Action<>
        __result = new DropdownSettings(get_current: () => UpgradeUI_GetCurrent_Reverse(__instance), items: _items, on_select: (item) => UpgradeUI_OnVehicleSelect_Reverse(__instance, item));
        __result.content = grid;

        return false; // skip original
    }

    [HarmonyPatch("GetCurrent"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int UpgradeUI_GetCurrent_Reverse(UpgradeUI __instance) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. UpgradeUI_GetCurrent_Reverse");

    [HarmonyPatch("OnVehicleSelect"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UpgradeUI_OnVehicleSelect_Reverse(UpgradeUI __instance, SimpleDropdownItem item) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. UpgradeUI_OnVehicleSelect_Reverse");
}
