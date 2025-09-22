using System.Runtime.CompilerServices;
using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(CreateNewRouteAction))]
public static class CreateNewRouteAction_Patches
{
    [HarmonyPatch("GenerateVehiclesSelection", [typeof(Action < ExplorerVehicleEntity >), typeof(Func<ExplorerVehicleEntity, bool>), typeof(NewRouteSettings), typeof(IControl), typeof(GameScene), typeof(bool), typeof(string), typeof(byte), typeof(long)])]
    [HarmonyPrefix]
    public static bool CreateNewRouteAction_GenerateVehiclesSelection_Prefix(Action<ExplorerVehicleEntity> on_select, Func<ExplorerVehicleEntity, bool> is_selected, NewRouteSettings route, IControl parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L)
    {
        {
            Log.Write($"name={route.Name} cities={route.Cities.Count} vehicle={route.vehicle} price={price_adjust} typ={type}");

            // create the window
            ExplorerUI<ExplorerVehicleEntity> _explorer = new ExplorerUI<ExplorerVehicleEntity>(
                new string[10] // categories
                {
                    Localization.GetGeneral("name"),
                    Localization.GetCompany("company"),
                    Localization.GetGeneral("capacity"),
                    Localization.GetStats("speed"),
                    Localization.GetGeneral("inventory"),
                    Localization.GetGeneral("price"),
                    // MODDED
                    "profit", // 6 Localization.GetVehicle("estimated_profit")
                    "effic", // 7 Localization.GetGeneral("efficiency")
                    "through", // 8 Localization.GetGeneral("passengers")
                    "range", // 9 Localization.GetGeneral("range")
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
            if (route != null)
            {
                _explorer.city = route.Cities.TryGet(0, null);
            }
            if (type == 0 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetRoadVehicles_Reverse(route, scene, price_adjust));
            }
            if (type == 1 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetTrains_Reverse(route, scene, price_adjust));
            }
            if (type == 2 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetPlanes_Reverse(route, scene, price_adjust));
            }
            if (type == 3 || type == byte.MaxValue)
            {
                _explorer.AddItems(CreateNewRouteAction_GetShips_Reverse(route, scene, price_adjust));
            }
            if (above)
            {
                _explorer.AddToControlAbove(parent);
            }
            else
            {
                _explorer.AddToControlAuto(parent);
            }
        }

        return false; // skip original
    }

    [HarmonyPatch("GetTooltip"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CreateNewRouteAction_GetTooltip_Reverse(IControl parent, int category, GameScene scene) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetTooltip_Reverse");


    [HarmonyPatch("GetFilterCategories"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static FilterCategory[] CreateNewRouteAction_GetFilterCategories_Reverse(GameScene scene) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetFilterCategories_Reverse");

    [HarmonyPatch("GetRoadVehicles"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetRoadVehicles_Reverse(NewRouteSettings route, GameScene scene, long price_adjust) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetRoadVehicles_Reverse");

    [HarmonyPatch("GetTrains"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]

    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetTrains_Reverse(NewRouteSettings route, GameScene scene, long price_adjust) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetTrains_Reverse");

    [HarmonyPatch("GetPlanes"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetPlanes_Reverse(NewRouteSettings route, GameScene scene, long price_adjust) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetPlanes_Reverse");

    [HarmonyPatch("GetShips"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static GrowArray<ExplorerVehicleEntity> CreateNewRouteAction_GetShips_Reverse(NewRouteSettings route, GameScene scene, long price_adjust) =>
        // its a stub so it has no initial content
        throw new NotImplementedException("ERROR. CreateNewRouteAction_GetShips_Reverse");
}
