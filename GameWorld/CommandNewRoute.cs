using HarmonyLib;
using STM;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Commands;
using STM.GameWorld.Users;
using Utilities;

namespace UITweaks.GameWorld;


[HarmonyPatch(typeof(CommandNewRoute))]
public static class CommandNewRoute_Patches
{
    // Patches the situation when there is no free hub on the new route,
    // however it is an upgrade and the hub can be re-used
    [HarmonyPatch("Apply"), HarmonyPrefix]
    public static bool CommandNewRoute_Apply_Prefix(CommandNewRoute __instance, GameScene scene, NewRouteSettings ___settings, ushort ___manager, bool ___open)
    {
        // Process hub
        Hub? _hub = ___settings.upgrade?.GetHub(__instance.Company, scene);
        if (___manager < ushort.MaxValue)
        {
            _hub = scene.Cities[___manager].User.GetHub(__instance.Company);
        }
        else if (_hub == null) // no manager, no stored hub i.e. not an upgrade
        {
            for (int j = 0; j < ___settings.Cities.Count; j++)
            {
                Hub _h = ___settings.Cities[j].GetHub(__instance.Company);
                if (_h != null && _h.Vehicles.Count < _h.Max_vehicles)
                {
                    _hub = _h;
                    break;
                }
            }
        }
        if (_hub == null)
        {
            Log.Write("Cannot find the hub!", true);
            return false; // DANGER!
        }
        if (_hub.Vehicles.Count >= _hub.Max_vehicles)
        {
            CommandUpgradeHub commandUpgradeHub = new CommandUpgradeHub(_hub.Company, _hub.City);
            commandUpgradeHub.force = true;
            commandUpgradeHub.Apply(scene);
            if (_hub.Vehicles.Count >= _hub.Max_vehicles)
            {
                Log.Write("Failed to force-upgrade the hub!", true);
                return false; // DANGER!
            }
        }

        // Check wealth & ship status
        Company _company = scene.Session.Companies[__instance.Company];
        VehicleBaseEntity _vehicle = NewRouteSettings.GetVehicleEntity(___settings.vehicle);
        long _wealth = ((___manager == ushort.MaxValue || _hub.Manager == null) ? (_company.Wealth + _company.Frame_balance) : _hub.Manager.Budget);
        // Infixo: in original code there is a minus sign, which is weird since price is always positive
        if (___manager == ushort.MaxValue && _wealth < _vehicle.Price)
        {
            Log.Write("Not enough wealth to buy a new vehicle!", true);
            return false; // DANGER!
        }
        if (_vehicle is ShipEntity && !__instance.CallPrivateMethod<bool>("CanBeShip", []))
        {
            Log.Write("Trying to put a ship on land!", true);
            return false; // DANGER!
        }

        // ??
        for (int i = 1; i < ___settings.Cities.Count; i++)
        {
            ___settings.Cities[0].Destinations.Mark(___settings.Cities[i]);
            ___settings.Cities[i].Destinations.Mark(___settings.Cities[0]);
            ___settings.Cities[i - 1].Destinations.Mark(___settings.Cities[i]);
            ___settings.Cities[i].Destinations.Mark(___settings.Cities[i - 1]);
        }

        // PLANE
        if (_vehicle is PlaneEntity _plane_entity)
        {
            PlaneUser _user4 = _company.BuyPlane(new BuyVehicleSettings(_plane_entity, new Route(___settings), scene, _hub), (___manager == ushort.MaxValue || _hub.Manager == null) ? null : _hub.Manager);
            if (___settings.Name != null)
            {
                _user4.SetName(___settings.Name);
            }
            _company.Line_manager.AddVehicleToLine(_user4, scene);
            ___settings.upgrade?.Apply(_user4, scene);
            if (_user4.Company == scene.Session.Player)
            {
                scene.Session.Tasks.AddUpgraded(_user4);
            }
            if (_user4.Company == scene.Session.Player && ___manager == ushort.MaxValue && _company.AI == null)
            {
                if (___open)
                {
                    _user4.Select(scene);
                }
                scene.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Connection.SendCounter("veh_" + _vehicle.Name);
                scene.Statistics.MaxCounter("planes_max", _company.Planes);
                Main.Statistics.MaxCounter("planes_max", _company.Planes);
                Main.Connection.SendCounter("planes_max", _company.Planes, add: true, max: true);
            }
        }
        // ROAD VEHICLE
        else if (_vehicle is RoadVehicleEntity _road_vehicle_entity)
        {
            RoadVehicleUser _user3 = _company.BuyRoadVehicle(new BuyVehicleSettings(_road_vehicle_entity, new Route(___settings), scene, _hub), (___manager == ushort.MaxValue || _hub.Manager == null) ? null : _hub.Manager);
            if (___settings.Name != null)
            {
                _user3.SetName(___settings.Name);
            }
            _company.Line_manager.AddVehicleToLine(_user3, scene);
            ___settings.upgrade?.Apply(_user3, scene);
            if (_user3.Company == scene.Session.Player)
            {
                scene.Session.Tasks.AddUpgraded(_user3);
            }
            if (_user3.Company == scene.Session.Player && ___manager == ushort.MaxValue && _company.AI == null)
            {
                if (___open)
                {
                    _user3.Select(scene);
                }
                scene.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Connection.SendCounter("veh_" + _vehicle.Name);
                scene.Statistics.MaxCounter("road_v_max", _company.Road_vehicles);
                Main.Statistics.MaxCounter("road_v_max", _company.Road_vehicles);
                Main.Connection.SendCounter("road_v_max", _company.Road_vehicles, add: true, max: true);
                __instance.CallPrivateMethodVoid("CheckForRoadAchievements", [scene]);
            }
        }
        //SHIP
        else if (_vehicle is ShipEntity _ship_entity)
        {
            ShipUser _user2 = _company.BuyShip(new BuyVehicleSettings(_ship_entity, new Route(___settings), scene, _hub), (___manager == ushort.MaxValue || _hub.Manager == null) ? null : _hub.Manager);
            if (___settings.Name != null)
            {
                _user2.SetName(___settings.Name);
            }
            _company.Line_manager.AddVehicleToLine(_user2, scene);
            ___settings.upgrade?.Apply(_user2, scene);
            if (_user2.Company == scene.Session.Player)
            {
                scene.Session.Tasks.AddUpgraded(_user2);
            }
            if (_user2.Company == scene.Session.Player && ___manager == ushort.MaxValue && _company.AI == null)
            {
                if (___open)
                {
                    _user2.Select(scene);
                }
                scene.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Connection.SendCounter("veh_" + _vehicle.Name);
                scene.Statistics.MaxCounter("ships_max", _company.Ships);
                Main.Statistics.MaxCounter("ships_max", _company.Ships);
                Main.Connection.SendCounter("ships_max", _company.Ships, add: true, max: true);
            }
        }
        // TRAIN
        else if (_vehicle is TrainEntity _train)
        {
            TrainUser _user = _company.BuyTrain(new BuyVehicleSettings(_train, new Route(___settings), scene, _hub), (___manager == ushort.MaxValue || _hub.Manager == null) ? null : _hub.Manager);
            if (___settings.Name != null)
            {
                _user.SetName(___settings.Name);
            }
            _company.Line_manager.AddVehicleToLine(_user, scene);
            ___settings.upgrade?.Apply(_user, scene);
            if (_user.Company == scene.Session.Player)
            {
                scene.Session.Tasks.AddUpgraded(_user);
            }
            if (_user.Company == scene.Session.Player && ___manager == ushort.MaxValue && _company.AI == null)
            {
                if (___open)
                {
                    _user.Select(scene);
                }
                scene.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Statistics.ChangeCounter("veh_" + _vehicle.Name);
                Main.Connection.SendCounter("veh_" + _vehicle.Name);
                scene.Statistics.MaxCounter("trains_max", _company.Trains);
                Main.Statistics.MaxCounter("trains_max", _company.Trains);
                Main.Connection.SendCounter("trains_max", _company.Trains, add: true, max: true);
                __instance.CallPrivateMethodVoid("CheckForTrainAchievements", [scene]);
            }
        }

        // Stats
        if (_hub.Company == scene.Session.Player)
        {
            scene.Statistics.MaxCounter("vehicles_max", _company.Vehicles);
            Main.Statistics.MaxCounter("vehicles_max", _company.Vehicles);
            Main.Connection.SendCounter("vehicles_max", _company.Vehicles, add: true, max: true);
            scene.Session.Commands.vehicles_changed = true;
        }

        return false;
    }
}
