using HarmonyLib;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STMG.UI.Control;
using STVisual.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestMod;

[HarmonyPatch(typeof(UpgradeUI))]
public static class UpgradeUI_Patches
{
    [HarmonyPatch("Get"), HarmonyPrefix]
    public static bool UpgradeUI_Get_Prefix(UpgradeUI __instance, IControl parent, VehicleBaseUser vehicle, GameScene scene)
    {
        Log.Write($"{vehicle.ID} {vehicle.Type} c={vehicle.Company} s={vehicle.Speed} t={vehicle.Throughput.GetLastMonth()} b={vehicle.Balance.GetLastMonth()}");
        //Log.WriteCallingStack(10);
        return true; // continue
    }

    // this method fills up available vehicles; all fields are private in this class
    [HarmonyPatch("GetVehiclesOptions"), HarmonyPrefix]
    public static bool UpgradeUI_GetVehiclesOptions_Prefix(
        UpgradeUI __instance, ref DropdownSettings __result,
        GrowArray<VehicleBaseEntity> ___options, // private field
        Grid grid)
    {
        Log.Write($"grid {grid.Columns_count}x{grid.Rows_count} n={grid.Items_count}");
        GrowArray<SimpleDropdownItem> _items = new GrowArray<SimpleDropdownItem>();
        for (int i = 0; i < ___options.Count; i++)
        {
            _items.Add(new SimpleDropdownItem(i, ___options[i].GetNameWithIcons()));
        }
        // magic here, to pass Func<int> we must go through lambda
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
