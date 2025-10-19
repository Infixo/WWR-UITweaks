using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;
using System.Runtime.CompilerServices;
using Utilities;

namespace UITweaks.UIExplorer;


[HarmonyPatch(typeof(ExplorerLine))]
public static class ExplorerLine_Patches
{
    // Data extensions
    public class ExtraData
    {
        public double Distance;
        public int Age;
        //public long Waiting = -1L;
        public string Country = "";
        //public bool Active = true;
    }
    private static readonly ConditionalWeakTable<ExplorerLine, ExtraData> _extras = [];
    public static ExtraData Extra(this ExplorerLine line) => _extras.GetOrCreateValue(line);


    [HarmonyPatch(typeof(InfoUI), "GetRoutesCategories"), HarmonyPrefix]
    public static bool InfoUI_GetRoutesCategories_Prefix(ref string[] __result)
    {
        __result =
        [
        Localization.GetGeneral("name"), // 0
        Localization.GetVehicle("route"), // 1
        Localization.GetInfrastructure("vehicles"), // 2 <!cicon_road_vehicle><!cicon_train><!cicon_plane><!cicon_ship>"
        Localization.GetGeneral("efficiency"), // 3 current month
        Localization.GetGeneral("balance"), // 4
        // added
        "<!cicon_city>", // 5 num cities
        "<!cicon_left>  <!cicon_right>", // 6 length
        "<!cicon_fast>", // 7 age
        "<!cicon_ship_b>" // 8 evaluation "<!cicon_passenger>", // 8
        ];
        return false;
    }


    [HarmonyPatch(typeof(InfoUI), "GetRoutesFilterCategories"), HarmonyPrefix]
    public static bool InfoUI_GetRoutesFilterCategories_Postix(InfoUI __instance, ref FilterCategory[] __result)
    {
        //__result[0].Items[0].SetValue(0); // show empty lines
        __result = new FilterCategory[3]
        {
            // 0 Number of vehicles
            new FilterCategory(
                Localization.GetInfrastructure("vehicles"), "values", 
                new FilterCategoryItem(Localization.GetGeneral("min"), 1L), 
                new FilterCategoryItem(Localization.GetGeneral("max"), 
                __instance.CallPrivateMethod<int>("GetMaxRouteVehicles", []))),
            // 1 Vehicle type
            new FilterCategory(
                Localization.GetGeneral("category"), "list", 
                new FilterCategoryItem(Localization.GetInfrastructure("road_vehicles")), 
                new FilterCategoryItem(Localization.GetInfrastructure("trains")), 
                new FilterCategoryItem(Localization.GetInfrastructure("planes")), 
                new FilterCategoryItem(Localization.GetInfrastructure("ships"))),
            // 2 Categories: Empty, National, Evaluated
            new FilterCategory(
                "Filters", "list", // name + type
                //new FilterCategoryItem("- Reverse -"),
                new FilterCategoryItem("Empty"),
                new FilterCategoryItem("National"),
                new FilterCategoryItem("International"),
                new FilterCategoryItem("Evaluated")),
        };
        __result[0].Items[0].SetValue(1L);
        return false;
    }

    /*
    // Simple stopwatch and counters to measure ExplorerLine performance
    public static Stopwatch sw = new();

    [HarmonyPatch(typeof(InfoUI), "GetRoutes"), HarmonyPrefix]
    public static bool InfoUI_GetRoutes_Prefix()
    {
        sw.Reset(); sw.Start();
        WorldwideRushExtensions.CounterIsConn = 0;
        WorldwideRushExtensions.CounterGetLine0 = 0;
        WorldwideRushExtensions.CounterGetLine1 = 0;
        WorldwideRushExtensions.CounterGetLine2 = 0;
        WorldwideRushExtensions.CounterGetLine3 = 0;
        WorldwideRushExtensions.CounterGetPath = 0;
        return true; // continue
    }

    [HarmonyPatch(typeof(InfoUI), "GetRoutes"), HarmonyPostfix]
    public static void InfoUI_GetRoutes_Postfix()
    {
        sw.Stop();
        Log.Write($"Elapsed time: {sw.ElapsedMilliseconds} ms, IC={WorldwideRushExtensions.CounterIsConn} GP={WorldwideRushExtensions.CounterGetPath}");
        Log.Write($"Counters: 0= {WorldwideRushExtensions.CounterGetLine0} 1={WorldwideRushExtensions.CounterGetLine1} 2={WorldwideRushExtensions.CounterGetLine2} 3={WorldwideRushExtensions.CounterGetLine3} ");
    }
    */


    [HarmonyPatch(typeof(InfoUI), "GetRouteTooltip"), HarmonyPrefix]
    public static bool InfoUI_GetRouteTooltip_Prefix(IControl parent, int id, Session ___Session)
    {
        TooltipPreset? _tooltip = null;
        switch (id)
        {
            case 0:
            case 1:
                _tooltip = GeneralTooltips.GetRoute(___Session.Scene.Engine);
                break;
            case 2:
                _tooltip = GeneralTooltips.GetVehicles(___Session.Scene.Engine);
                break;
            case 3: // Efficiency
                _tooltip = TooltipPreset.Get(Localization.GetGeneral("efficiency"), ___Session.Scene.Engine);
                _tooltip.AddDescription("Current month efficiency.");
                break;
            case 4: // Balance
                _tooltip = GeneralTooltips.GetBalance(___Session.Scene.Engine);
                _tooltip.AddSeparator();
                _tooltip.AddDescription(Localization.GetInfo("balance_quarter"));
                break;
            case 5: // Cities 
                _tooltip = TooltipPreset.Get(Localization.GetCity("cities"), ___Session.Scene.Engine);
                _tooltip.AddDescription("Number of cities.");
                break;
            case 6: // Length
                _tooltip = TooltipPreset.Get(Localization.GetGeneral("distance"), ___Session.Scene.Engine);
                _tooltip.AddDescription("Total distance.");
                break;
            case 7: // Age
                _tooltip = TooltipPreset.Get("Line age", ___Session.Scene.Engine);
                //_tooltip.AddDescription("Estimated throughput based on vehicles' speeds and capacities. How many passengers can be transported during a month assuming full capacity usage.");
                _tooltip.AddDescription("Age of the oldest vehicle (in months).");
                break;
            case 8: // Waiting
                _tooltip = TooltipPreset.Get("Line evaluation", ___Session.Scene.Engine);
                _tooltip.AddDescription(AITweaksLink.Active ? "Marks with <!cicon_ship_b> evaluated lines." : "AITweaks not present.");
                break;
        }
        _tooltip?.AddToControlBellow(parent);
        return false;
    }


    // Line ID without commas!
    [HarmonyPatch(typeof(Line), "GetName"), HarmonyPrefix]
    public static bool Line_GetName_Prefix(Line __instance, ref string __result, string ___name, bool prefix = true)
    {
        string _prefix = prefix ? WorldwideRushExtensions.GetVehicleTypeIcon(__instance.Vehicle_type) : "";
        if (___name != null)
        {
            __result = _prefix + ___name;
            return false;
        }
        if (__instance.Instructions.Cities.Length > 2)
            __result = $"{_prefix}{__instance.ID + 1}. {__instance.Instructions.Cities[0].Name} <!cicon_right>...<!cicon_right> {__instance.Instructions.Cities[__instance.Instructions.Cities.Length - 1].Name}";
        else
            __result = $"{_prefix}{__instance.ID + 1}. {__instance.Instructions.Cities[0].Name} <!cicon_right> {__instance.Instructions.Cities[__instance.Instructions.Cities.Length - 1].Name}";
        return false;
    }


    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerLine_GetMainControl_Prefix(ExplorerLine __instance, ref Button ___main_button, ref Image ___alt, GameScene scene)
    {
        // define more labels
        Label[] tmpLabels = new Label[9];
        __instance.SetPublicProperty("Labels", tmpLabels);

        // control - button
        int _height = 32;
        ___main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, _height, 1f), scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: false, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        ___main_button.Opacity = 0f;
        ___main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        Button butTemp = ___main_button;
        ___main_button.OnMouseStillTime += (Action)delegate
        {
            __instance.CallPrivateMethodVoid("GetTooltip", [scene]);
        };

        ___alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        ___alt.Opacity = 0f;
        _collection.Transfer(___alt);

        // control - grid
        Grid main_grid = new Grid(ContentRectangle.Stretched, __instance.Labels.Length, 1, SizeType.Weight);
        __instance.SetPrivateField("main_grid", main_grid);
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
        var (countryTxt, routeTxt) = __instance.GetCurrentRouteEx(scene);
        __instance.Extra().Country = countryTxt;
        Label _route = LabelPresets.GetDefault(routeTxt, scene.Engine);
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

        // check if there are >1 hubs
        Dictionary<ushort, int> hubs = [];
        for (int i = 0; i < __instance.Line.Routes.Count; i++)
        {
            VehicleBaseUser veh = __instance.Line.Routes[i].Vehicle;
            hubs.TryAdd(veh.Hub.City, 0);
            hubs[veh.Hub.City]++;
        }
        if (hubs.Count > 1)
        {
            __instance.Labels[2].Text += $"  <!cicon_storage>{hubs.Count}";
            __instance.Labels[2].Color = LabelPresets.Color_negative;
        }

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
        InsertLabelAt(5, StrConversions.CleanNumber(__instance.Line.Instructions.Cities.Length) + (__instance.Line.Instructions.Cyclic ? " <!cicon_fast>" : ""));

        // 6 Length
        __instance.Extra().Distance = __instance.Line.GetTotalDistance();
        InsertLabelAt(6, StrConversions.GetDistance(__instance.Extra().Distance));

        // 7 Age of the line
        __instance.Extra().Age = __instance.Line.GetAge(); // GetQuarterAverageThroughput(); // EstimateThroughput();
        InsertLabelAt(7, StrConversions.CleanNumber(__instance.Extra().Age));

        // 8 Evaluation
        //__instance.Extra().Waiting = __instance.Line.GetWaiting();
        if (AITweaksLink.Active)
            InsertLabelAt(8, AITweaksLink.GetNumEvaluations(__instance.Line) > 0 ? "<!cicon_ship_b>" : ".");
        else
            InsertLabelAt(8, "");

        return false;
    }


    /// <summary>
    /// Age of the line - age of the oldest vehicle.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    internal static int GetAge(this Line line)
    {
        int age = 0;
        for (int i = 0; i < line.Vehicles; i++)
            age = Math.Max(age, line.Routes[i].Vehicle.Age);
        return age;
    }

    /*
    [HarmonyPatch("Matches"), HarmonyPostfix]
    public static void ExplorerLine_Matches_Postfix(ExplorerLine __instance, bool __result, FilterCategory[] categories, GameScene scene, Company company, CityUser city)
    {
        __instance.Extra().Active = __result;
    }

    [HarmonyPatch("Update"), HarmonyPostfix]
    public static void ExlorerLine_Update_Postfix(ExplorerLine __instance, GameScene scene, Company company)
    {
        if (!__instance.Extra().Active || __instance.Extra().Waiting != -1L) return;
        __instance.Extra().Waiting = __instance.Line.GetWaiting();
        __instance.Labels[8].Text = StrConversions.CleanNumber(__instance.Extra().Waiting);
        //Log.Write($"{scene.Session.Frame&0xFFFF:X4} {__instance.Line.ID:D4}");
    }
    */

    public static int EstimateThroughput(this Line line)
    {
        // Calculate throughput based on actual vehicles and route distances
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
        float numTrips = 24f / ((float)distance / averageSpeed + (float)(numStops-1) * (float)stationTime / 3600f);
        return line.Vehicles * (int)(averageCapacity * numTrips);
    }


    // The modded version adds and store country name, so lines can be sorted and filtered by country also
    public static (string,string) GetCurrentRouteEx(this ExplorerLine line, GameScene scene)
    {
        Dictionary<byte, int> countries = []; // Key=country id, Value=occurences
        string _text = "";
        for (int i = 0; i < line.Line.Instructions.Cities.Length; i++)
        {
            if (i > 0)
            {
                _text += " <!cicon_right> ";
            }
            _text += line.Line.Instructions.Cities[i].GetNameWithIcon(scene);
            byte country_id = line.Line.Instructions.Cities[i].City.Country_id;
            countries.TryAdd(country_id, 0);
            countries[country_id]++;
        }

        // decide which country it is
        byte lineCountry = line.Line.Instructions.Cities[0].City.Country_id;
        var maxPair = countries.Aggregate((l, r) => l.Value > r.Value ? l : r);
        if (maxPair.Value > countries[lineCountry])
            lineCountry = maxPair.Key;

        // append country name and return both
        string countryName = scene.Countries[lineCountry].Name.GetTranslation(Localization.Language);
        _text = countryName + ":  " + _text;
        return (countryName, _text);
    }


    [HarmonyPatch("Smaller"), HarmonyPrefix]
    public static bool ExplorerLine_Smaller_Prefix(ExplorerLine __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        ExplorerLine _item = (ExplorerLine)item;
        if (__instance.Valid != _item.Valid)
        {
            __result = __instance.Valid.CompareTo(_item.Valid) > 0;
            return false;
        }

        int result = 0; // temporary comparison, for normal order result<0, for reversed order resut>0
        switch (sort_id % __instance.Labels.Length) // sort column
        {
            case 0:
                if (__instance.Line.Has_name && _item.Line.Has_name)
                    result = __instance.Labels[0].Text.CompareTo(_item.Labels[0].Text);
                break;

            case 1:
                //result = __instance.Labels[1].Text.CompareTo(_item.Labels[1].Text);
                result = __instance.Extra().Country.CompareTo(_item.Extra().Country);
                break;

            case 2: // num vehicles
                result = __instance.Line.Vehicles - _item.Line.Vehicles;
                break;

            case 3: // current month efficieny
                result = __instance.GetPrivateField<float>("efficiency").CompareTo(_item.GetPrivateField<float>("efficiency"));
                break;

            case 4: // balance
                result = __instance.GetPrivateField<long>("balance").CompareTo(_item.GetPrivateField<long>("balance"));
                break;

            case 5: // num cities
                result = __instance.Line.Instructions.Cities.Length.CompareTo(_item.Line.Instructions.Cities.Length);
                break;

            case 6: // length
                result = __instance.Extra().Distance.CompareTo(_item.Extra().Distance);
                break;

            case 7: // age
                result = __instance.Extra().Age.CompareTo(_item.Extra().Age);
                break;

            case 8: // evaluated
                //result = __instance.Extra().Waiting.CompareTo(_item.Extra().Waiting);
                result = 0; // no comparison here
                break;
        }

        // fallback for sorting - by ID
        if (result == 0)
            result = __instance.Line.ID.CompareTo(_item.Line.ID);

        // Normal order: sortid < length, Reverse order: sortid >= length; default is descending
        __result = (sort_id < __instance.Labels.Length) ? result > 0 : result < 0;

        return false;
    }

    [HarmonyPatch("Matches"), HarmonyPrefix]
    public static bool ExplorerLine_Matches_Prefix(ExplorerLine __instance, ref bool __result, FilterCategory[] categories, GameScene scene, Company company, CityUser city)
    {
        __result = false;
        // 0 Number of vehicles
        if (categories[2].Items[0].Selected && __instance.Line.Vehicles > 0)
            return false;
        if (!categories[2].Items[0].Selected && !categories[0].MinMaxFits(__instance.Line.Vehicles))
            return false;

        // 1 Vehicle type
        if (categories[1].HasSelected())
        {
            if (categories[1].Items[0].Selected && __instance.Line.HasRoadVehicles())
                __result = true;
            if (categories[1].Items[1].Selected && __instance.Line.HasTrains())
                __result = true;
            if (categories[1].Items[2].Selected && __instance.Line.HasPlanes())
                __result = true;
            if (categories[1].Items[3].Selected && __instance.Line.HasShips())
                __result = true;
        }
        else
            __result = true;

        // 2 Filters
        bool national = __instance.Line.IsNational();
        if (categories[2].Items[1].Selected)
            __result &= national;
        if (categories[2].Items[2].Selected)
            __result &= !national;
        if (categories[2].Items[3].Selected)
            __result &= AITweaksLink.GetNumEvaluations(__instance.Line) > 0;

        return false;
    }


    internal static bool IsNational(this Line line)
    {
        // Check if all cities are in the same country
        byte country = line.Instructions.Cities[0].City.Country_id;
        foreach (CityUser city in line.Instructions.Cities)
            if (city.City.Country_id != country)
                return false;
        return true;
    }


    [HarmonyPatch("FillCategories"), HarmonyPostfix]
    public static void ExplorerLine_FillCategories_Postfix(ExplorerLine __instance, FilterCategory[] categories)
    {
        if (__instance.Valid)
        {
            // Empty
            if (__instance.Line.Vehicles == 0)
                categories[2].Items[0].IncreaseCount();
            // National/International
            if (__instance.Line.IsNational())
                categories[2].Items[1].IncreaseCount();
            else
                categories[2].Items[2].IncreaseCount();
            // Evaluated
            if (AITweaksLink.GetNumEvaluations(__instance.Line) > 0)
                categories[2].Items[3].IncreaseCount();
        }
    }
}
