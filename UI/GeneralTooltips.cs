using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.UI;
using STMG.Engine;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(GeneralTooltips))]
public static class GeneralTooltips_Patches
{
    [HarmonyPatch("GetVehicle"), HarmonyPostfix]
    public static void GeneralTooltips_GetVehicle_Postfix(TooltipPreset __result, VehicleBaseEntity entity, GameEngine engine, int distance, int longitude, Country country)
    {
        int minEff = entity is TrainEntity train ? entity.Real_min_passengers * 100 / train.Max_capacity : entity.Real_min_passengers * 100 / entity.Capacity;
        __result.AddStatsLine("Throughput @ 1000km", "<!cicon_passenger><!cicon_passenger> " + StrConversions.CleanNumber(entity.GetEstimatedThroughput()));
        __result.AddStatsLine("<!cl:minimum_efficiency:" + Localization.GetVehicle("minimum_efficiency") + ">", StrConversions.CleanNumber(minEff) + "%");
    }

    [HarmonyPatch("GetRoadVehicles"), HarmonyPrefix]
    public static bool GeneralTooltips_GetRoadVehicles_Prefix(ref TooltipPreset __result, GameEngine engine)
    {
        __result = GetVehicleEntities<RoadVehicleEntity>("road_vehicles", MainData.Road_vehicles, engine);
        return false;
    }

    [HarmonyPatch("GetTrains"), HarmonyPrefix]
    public static bool GeneralTooltips_GetTrains_Prefix(ref TooltipPreset __result, GameEngine engine)
    {
        __result = GetVehicleEntities<TrainEntity>("trains", MainData.Trains, engine);
        return false;
    }

    [HarmonyPatch("GetPlanes"), HarmonyPrefix]
    public static bool GeneralTooltips_GetPlanes_Prefix(ref TooltipPreset __result, GameEngine engine)
    {
        __result = GetVehicleEntities<PlaneEntity>("planes", MainData.Planes, engine);
        return false;
    }

    [HarmonyPatch("GetShips"), HarmonyPrefix]
    public static bool GeneralTooltips_GetShips_Prefix(ref TooltipPreset __result, GameEngine engine)
    {
        __result = GetVehicleEntities<ShipEntity>("ships", MainData.Ships, engine);
        return false;
    }

    public static TooltipPreset GetVehicleEntities<T>(string type, T[] vehicles, GameEngine engine) where T : VehicleBaseEntity
    {
        // Header
        TooltipPreset _tooltip = TooltipPreset.Get(Localization.GetInfrastructure(type), engine, can_lock: true);
        _tooltip.AddDescription(Localization.GetInfo(type));
        _tooltip.AddSeparator();
        _tooltip.AddDescription(Localization.GetInfo("vehicle_price"));
        _tooltip.AddSeparator();
        _tooltip.AddBoldLabel(Localization.GetInfrastructure("vehicles"));
        GameScene obj = (GameScene)engine.Main_scene;
        Company _company = obj.Session.GetPlayer();
        if (!obj.Settings.VehicleIsValid(vehicles[0]))
        {
            return _tooltip;
        }
        // Sorted list of vehicles
        static bool Smaller(T a, T b)
        {
            int result = a.Company.Entity.Translated_name.CompareTo(b.Company.Entity.Translated_name);
            if (result == 0)
                result = a.ID.CompareTo(b.ID);
            return result < 0;
        }
        GrowArray<T> _vehicles = new GrowArray<T>();
        _vehicles.Add(vehicles);
        //_vehicles.Sort((T a, T b) => a.Translated_name.CompareTo(b.Translated_name) < 0);
        _vehicles.Sort(Smaller);
        VehicleCompanyEntity? vce = null;
        for (int i = 0; i < _vehicles.Count; i++)
        {
            // Vehicle company line
            if (_vehicles[i].Company.Entity != vce)
            {
                vce = _vehicles[i].Company.Entity;
                _tooltip.AddStatsLine(
                    $"<!cl:e.{vce.Name}:{vce.Translated_name}> ({vce.Country.Item.Name.GetTranslation(Localization.Language)})",
                    $"{_company.Loyalty.GetVehicles(vce)} {Localization.GetInfrastructure("vehicles")}");
            }
            _tooltip.AddStatsLine($"<!cl:e.{_vehicles[i].Name}:{_vehicles[i].GetNameWithIcons()}>", _vehicles[i].CanBuy(_company, -1000L) ? "" : ("<!cicon_lock> " + Localization.GetVehicle("locked")), i % 2 == 1);
        }
        return _tooltip;
    }
}
