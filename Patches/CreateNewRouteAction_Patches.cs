using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;
using System.Runtime.CompilerServices;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(CreateNewRouteAction))]
public static class CreateNewRouteAction_Patches
{
    [HarmonyPatch("GenerateVehiclesSelection", [typeof(Action < ExplorerVehicleEntity >), typeof(Func<ExplorerVehicleEntity, bool>), typeof(NewRouteSettings), typeof(IControl), typeof(GameScene), typeof(bool), typeof(string), typeof(byte), typeof(long), typeof(VehicleBaseUser[])])]
    [HarmonyPrefix]
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public static bool CreateNewRouteAction_GenerateVehiclesSelection_Prefix(Action<ExplorerVehicleEntity> on_select, Func<ExplorerVehicleEntity, bool> is_selected, NewRouteSettings route, IControl parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L, VehicleBaseUser[] replace = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    {
        {
            //Log.Write($"name={route.Name} cities={route.Cities.Count} vehicle={route.vehicle} price={price_adjust} typ={type}");

            // create the window
            ExplorerUI<ExplorerVehicleEntity> _explorer = new ExplorerUI<ExplorerVehicleEntity>(
                new string[11] // categories
                {
                    Localization.GetGeneral("name"), // 0
                    Localization.GetCompany("company"), // 1
                    "<!cicon_passenger>", //Localization.GetGeneral("capacity"), // 2
                    "Min<!cicon_passenger>", // 3
                    "Min%", // 4
                    Localization.GetStats("speed"), //5
                    "<!cicon_storage>", // Localization.GetGeneral("inventory"), //6
                    Localization.GetGeneral("price"), //7
                    "Profit", // 8 Localization.GetVehicle("estimated_profit")
                    "<!cicon_passenger><!cicon_passenger>", // 9 throughput
                    Localization.GetGeneral("range"), // 10
                },
                on_select,
                is_selected,
                scene.UI, // ui
                scene, // scene
                5, // sort_id
                history,
                delegate (IControl p, int c) // Action<IControl, int> tooltip
                {
                    CreateNewRouteAction_GetTooltip_Reverse(p, c, scene);
                },
                CreateNewRouteAction_GetFilterCategories_Reverse(scene));

            // -------------
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601
            if (route != null)
            {
                _explorer.city = route.Cities.TryGet(0, null);
            }
            if (type == 0 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetRoadVehicles_Reverse(route, scene, price_adjust, replace));
                //_explorer.AddItems(ExtensionsHelper.CallPrivateMethod<GrowArray<ExplorerVehicleEntity>>(__instance, "GetRoadVehicles", [route, scene, price_adjust, replace]));
            }
            if (type == 1 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetTrains_Reverse(route, scene, price_adjust, replace));
                //_explorer.AddItems(ExtensionsHelper.CallPrivateMethod<GrowArray<ExplorerVehicleEntity>>(__instance, "GetTrains", [route, scene, price_adjust, replace]));
            }
            if (type == 2 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetPlanes_Reverse(route, scene, price_adjust, replace));
                //_explorer.AddItems(ExtensionsHelper.CallPrivateMethod<GrowArray<ExplorerVehicleEntity>>(__instance, "GetPlanes", [route, scene, price_adjust, replace]));
            }
            if (type == 3 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetShips_Reverse(route, scene, price_adjust, replace));
                //_explorer.AddItems(ExtensionsHelper.CallPrivateMethod<GrowArray<ExplorerVehicleEntity>>(__instance, "GetShips", [route, scene, price_adjust, replace]));
            }
            if (above)
            {
                _explorer.AddToControlAbove(parent);
            }
            else
            {
                _explorer.AddToControlAuto(parent);
            }
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8601
        }

        return false; // skip original
    }


#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

    [HarmonyPatch("GetTooltip"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CreateNewRouteAction_GetTooltip_Reverse(IControl parent, int category, GameScene scene) =>
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetTooltip_Reverse");

    [HarmonyPatch("GetFilterCategories"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static FilterCategory[] CreateNewRouteAction_GetFilterCategories_Reverse(GameScene scene) =>
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetFilterCategories_Reverse");

    [HarmonyPatch("GetRoadVehicles"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetRoadVehicles_Reverse(NewRouteSettings route, GameScene scene, long price_adjust, VehicleBaseUser[] replace = null) =>
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetRoadVehicles_Reverse");

    [HarmonyPatch("GetTrains"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]

    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetTrains_Reverse(NewRouteSettings route, GameScene scene, long price_adjust, VehicleBaseUser[] replace = null) =>
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetTrains_Reverse");

    [HarmonyPatch("GetPlanes"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetPlanes_Reverse(NewRouteSettings route, GameScene scene, long price_adjust, VehicleBaseUser[] replace = null) =>
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetPlanes_Reverse");

    [HarmonyPatch("GetShips"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetShips_Reverse(NewRouteSettings route, GameScene scene, long price_adjust, VehicleBaseUser[] replace = null) =>
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetShips_Reverse");

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

}
