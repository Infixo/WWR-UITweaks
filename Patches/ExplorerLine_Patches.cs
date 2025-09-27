using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STM.UI.Floating;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerLine))]
public static class ExplorerLine_Patches
{
    [HarmonyPatch(typeof(InfoUI), "GetRoutesCategories"), HarmonyPrefix]
    public static bool InfoUI_GetRoutesCategories_Prefix(ref string[] __result)
    {
        __result =
        [
        Localization.GetGeneral("name"), // 0
        Localization.GetVehicle("route"), // 1
        "<!cicon_road_vehicle><!cicon_train><!cicon_plane><!cicon_ship>", //Localization.GetInfrastructure("vehicles"), // 2
        Localization.GetGeneral("efficiency"), // 3
        Localization.GetGeneral("balance"), // 4
        // added
        "<!cicon_city>", // 5 num cities
        "<!cicon_left>  <!cicon_right>", // 6 length
        "<!cicon_fast>", // 7
        "<!cicon_passenger>", // 8
        ];
        return false;
    }


    [HarmonyPatch(typeof(InfoUI), "GetRouteTooltip"), HarmonyPostfix]
    public static void RouteUI_GetRouteTooltip_Postfix(IControl parent, int id, Session ___Session)
    {
        TooltipPreset? _tooltip = null;
        switch (id)
        {
            case 7:
                _tooltip = TooltipPreset.Get("Throughput", ___Session.Scene.Engine);
                _tooltip.AddDescription("Estimated throughput based on vehicles' speeds and capacities. How many passengers can be transported during a month assuming full capacity usage.");
                break;
            case 8:
                _tooltip = TooltipPreset.Get("Transport needs", ___Session.Scene.Engine);
                _tooltip.AddDescription("Estimated throughput needed to transport within a month passengers within a given line i.e. passengers wanting to use another line are excluded.");
                break;
        }
        _tooltip?.AddToControlBellow(parent);
    }


    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool GetMainControl(ExplorerLine __instance, ref Button ___main_button, ref Image ___alt, GameScene scene)
    {
        // define more labels
        Label[] tmpLabels = new Label[9];
        ExtensionsHelper.SetPublicProperty(__instance, "Labels", tmpLabels);

        // control - button
        int _height = 32;
        ___main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, _height, 1f), scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: false, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        ___main_button.Opacity = 0f;
        ___main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        Button butTemp = ___main_button;
        ___main_button.OnMouseStillTime += (Action)delegate
        {
            ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetTooltip", [scene]);
            // test
            TooltipPreset tt = TooltipPreset.Get("Debug", scene.Engine, can_lock: true);
            tt.AddDescription("test");
            tt.AddDescription("TEST TEST");
            tt.AddToControlBellow(butTemp);
        };

        ___alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        ___alt.Opacity = 0f;
        _collection.Transfer(___alt);

        // control - grid
        Grid main_grid = new Grid(ContentRectangle.Stretched, __instance.Labels.Length, 1, SizeType.Weight);
        ExtensionsHelper.SetPrivateField(__instance, "main_grid", main_grid);
        main_grid.OnFirstUpdate += (Action)delegate
        {
            main_grid.update_children = false;
        };
        main_grid.OnUpdate += (Action)delegate
        {
            main_grid[1].OnUpdate.Invoke();
        };
        _collection.Transfer(main_grid);

        // Helper
        void InsertLabelAt(int at, string text, HorizontalAlignment align = HorizontalAlignment.Center)
        {
            Label label = LabelPresets.GetDefault(text, scene.Engine);
            label.horizontal_alignment = align;
            label.Margin_local = new FloatSpace(MainData.Margin_content);
            main_grid.Transfer(label, at, 0);
            __instance.Labels[at] = label;
        }

        // 0 Name
        Label _name = LabelPresets.GetDefault(__instance.Line.GetName(), scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_name, 0, 0);
        __instance.Labels[0] = _name;

        // 1 Route
        Label _route = LabelPresets.GetDefault(ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetCurrentRoute", [scene]), scene.Engine);
        _route.Margin_local = new FloatSpace(MainData.Margin_content);
        IControl _radio = LabelPresets.GetRadio(_route, 400);
        _radio.Mouse_visible = false;
        main_grid.Transfer(_radio, 1, 0);
        __instance.Labels[1] = _route;

        // 2 Vehicles
        Label _vehicles = LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.Line.Vehicles), scene.Engine);
        _vehicles.Margin_local = new FloatSpace(MainData.Margin_content);
        _vehicles.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_vehicles, 2, 0);
        __instance.Labels[2] = _vehicles;

        // 3 Efficiency
        Label _efficiency = LabelPresets.GetDefault("100%", scene.Engine);
        _efficiency.Margin_local = new FloatSpace(MainData.Margin_content);
        _efficiency.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_efficiency, 3, 0);
        __instance.Labels[3] = _efficiency;

        // 4 Balance
        Label _balance = LabelPresets.GetDefault("WWWWWWW", scene.Engine);
        _balance.Margin_local = new FloatSpace(MainData.Margin_content);
        _balance.horizontal_alignment = HorizontalAlignment.Right;
        main_grid.Transfer(_balance, 4, 0);
        __instance.Labels[4] = _balance;

        // 5 Num cities
        InsertLabelAt(5, StrConversions.CleanNumber(__instance.Line.Instructions.Cities.Length) + (__instance.Line.Instructions.Cyclic ? " <!cicon_down>" : ""));

        // 6 Length
        InsertLabelAt(6, StrConversions.GetDistance(__instance.Line.GetTotalDistance()));

        // 7 Estimated vehicles throughput
        InsertLabelAt(7, StrConversions.CleanNumber(__instance.Line.EstimateThroughput()));

        // 8 Estimated through needed to transport currently waiting passangers within a month
        InsertLabelAt(8, "999");

        return false;
    }


    public static double GetTotalDistance(this Line line)
    {
        // Calculate total distance, for cyclic routes adds last-first section
        // Distance between cities is not stored, it is calculated when needed :(
        CityUser[] cities = line.Instructions.Cities; // for readability
        if (cities.Length < 2)
        {
            return 0.0; //  there is no route yet
        }

        static double GetDistance(CityUser a, CityUser b, byte vehicle_type)
        {
            // TODO: this could be cached later on to speed up the calculations
            switch (vehicle_type)
            {
                case 0: return RoadPathSearch.GetRoute(a, b).Distance;
                case 1: return RoadPathSearch.GetRails(a, b).Distance;
                case 2: return GameScene.GetDistance(a, b);
                case 3: return SeaPathSearch.GetRoute(a, b).Distance;
            }
            return 0d;
        }

        double distance = 0.0;
        for (int i = 1; i < cities.Length; i++)
        {
            double _dist = GetDistance(cities[i - 1], cities[i], line.Vehicle_type);
            distance += _dist;
        }
        if (line.Instructions.Cyclic)
        {
            double _dist = GetDistance(cities[^1], cities[0], line.Vehicle_type);
            distance += _dist;
        }
        return distance;
    }


    public static int EstimateThroughput(this Line line)
    {
        // Calculate throughput based on actual vehicles and route distances
        // What is needed:
        // a) average speed
        // b) average vehicle capacity, weighted with speed
        // c) total distance and number of stops
        // Formula: avg_capacity * 24 / ( (distance/average_speed) + num_stops * wait_time)

        if (line.Instructions.Cities.Length < 2 || line.Vehicles == 0)
        {
            return 0; //  there is no route yet or no vehicles assigned
        }

        double distance = line.GetTotalDistance();
        int numStops = line.Instructions.Cyclic ? line.Instructions.Cities.Length + 1 : line.Instructions.Cities.Length;

        // Get station wait time
        int stationTime = 0; // in seconds
        switch (line.Vehicle_type)
        {
            case 0: stationTime = MainData.Defaults.Bus_station_time; break;
            case 1: stationTime = MainData.Defaults.Train_station_time; break;
            case 2: stationTime = MainData.Defaults.Plane_airport_time; break;
            case 3: stationTime = MainData.Defaults.Ship_port_time; break;
        }

        // Iterate through vehicles and calculate average speed and weighted capacity
        int sumSpeed = 0;
        int sumCapacityWeighted = 0;
        for (int i = 0; i < line.Routes.Count; i++)
        {
            VehicleBaseEntity vbe = line.Routes[i].Vehicle.Entity_base;
            sumSpeed += vbe.Speed;
            sumCapacityWeighted += (vbe is TrainEntity train ? train.Max_capacity : vbe.Capacity) * vbe.Speed;
        }
        float averageSpeed = (float)sumSpeed / (float)line.Vehicles;
        float averageCapacity = (float)sumCapacityWeighted / (float)sumSpeed;

        // Calculate actual throughput
        float numTrips = 24f / ((float)distance / averageSpeed + (float)numStops * (float)stationTime / 3600f);
        return line.Vehicles * (int)(averageCapacity * numTrips);
    }

    /*
    [HarmonyPatch("GetTooltip"), HarmonyPrefix]
    public static bool ExplorerLine_GetTooltip_Prefix(ExplorerLine __instance, Button ___main_button, GameScene scene)
    {
        CityUI.GetLineBalance(___main_button, __instance.Line, scene);
        return false;
    }
    */
    /*
    [HarmonyPatch(""), HarmonyPrefix]
    public static bool Smaller(ExplorerLine __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        return true;
    }


    [HarmonyPatch(""), HarmonyPrefix]
    public static bool Update(ExplorerLine __instance, GameScene scene, Company company)
    {
        return true;
    }
    */
}
