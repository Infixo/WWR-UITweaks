using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;
using Utilities;

namespace UITweaks.UI;


[HarmonyPatch(typeof(CreateNewRouteAction))]
public static class CreateNewRouteAction_Patches
{
    [HarmonyPatch("GenerateVehiclesSelection", [typeof(Action<ExplorerVehicleEntity>), typeof(Func<ExplorerVehicleEntity, bool>), typeof(NewRouteSettings), typeof(IControl), typeof(GameScene), typeof(bool), typeof(string), typeof(byte), typeof(long), typeof(VehicleBaseUser[])])]
    [HarmonyPrefix]
    public static bool CreateNewRouteAction_GenerateVehiclesSelection_Prefix(CreateNewRouteAction __instance, Action<ExplorerVehicleEntity> on_select, Func<ExplorerVehicleEntity, bool> is_selected, NewRouteSettings route, IControl parent, GameScene scene, bool above, string history, byte flag, long price_adjust, VehicleBaseUser[] replace)
    {
        string[] categories =
        [
            Localization.GetGeneral("name"), // 0
            Localization.GetCompany("company"), // 1
            "<!cicon_passenger>", //Localization.GetGeneral("capacity"), // 2
            "Min<!cicon_passenger>", // 3
            "Min%", // 4
            Localization.GetStats("speed"), //5
            "<!cicon_storage>", // Localization.GetGeneral("inventory"), //6
            Localization.GetGeneral("price"), //7
            "Profit", // 8 Localization.GetVehicle("estimated_profit")
            "<!cicon_passenger><!cicon_fast>", // 9 throughput
            Localization.GetGeneral("range"), // 10
        ];

        flag &= scene.Settings.Vehicles_flag;
        ExplorerUI<ExplorerVehicleEntity> _explorer = new ExplorerUI<ExplorerVehicleEntity>(categories, on_select, is_selected, scene.UI, scene, 5, history, 
            delegate (IControl p, int c) { typeof(CreateNewRouteAction).CallPrivateStaticMethodVoid("GetTooltip", [p, c, scene]); },
            typeof(CreateNewRouteAction).CallPrivateStaticMethod<FilterCategory[]>("GetFilterCategories", [scene]));
        if (route != null)
        {
            _explorer.city = route.Cities.TryGet(0, null);
        }
        // Patch 1.1.15
        _explorer.AddItems(() => typeof(CreateNewRouteAction).CallPrivateStaticMethod<GrowArray<ExplorerVehicleEntity>>("GetAllVehicles", [route!, scene, price_adjust, flag, replace]));
        if (above)
        {
            _explorer.AddToControlAbove(parent);
        }
        else
        {
            _explorer.AddToControlAuto(parent);
        }
        return false; // skip original
    }


    // This method is only called from within CreateNewRoute in case of a route edit.
    // The vehicle entity it returns is used later to virtually check if the route is valid.
    // It causes problems with planes because it returns the one with lowest range which makes most routes are invalid when editing.
    [HarmonyPatch(typeof(Line), "GetEntity"), HarmonyPostfix]
    public static void GetEntity(Line __instance, ref VehicleBaseEntity __result)
    {
        // Only specific case is patched - no vehicles and plane route
        if (__instance.Vehicles == 0 && __instance.GetPrivateField<byte>("vehicle_type") == 2)
        {
            __result = MainData.Planes[^1];
        }
    }
}
