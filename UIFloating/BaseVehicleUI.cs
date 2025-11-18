using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Commands;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STM.UI.Floating;
using STM.UI.Stats;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using System.Reflection;
using Utilities;

namespace UITweaks.UIFloating;


[HarmonyPatch(typeof(BaseVehicleUI))]
public static class BaseVehicleUI_Patches
{
    [HarmonyPatch(typeof(BaseVehicleUI), MethodType.Constructor, [typeof(VehicleBaseUser), typeof(GameScene)]), HarmonyPostfix]
    public static void BaseVehicleUI_BaseVehicleUI_Postfix(BaseVehicleUI __instance, VehicleBaseUser vehicle, GameScene scene)
    {
        if (vehicle.Company != scene.Session.Player) return;
        // Add line number to the vehicle name
        LongText header = __instance.GetPrivateProperty<LongText>("Label_header");
        header.Text += " [" + ((vehicle.GetLine(scene)?.ID + 1) ?? 0).ToString() + "]";
    }


    [HarmonyPatch("GetControls"), HarmonyPrefix]
    public static bool BaseVehicleUI_GetControls_Prefix(BaseVehicleUI __instance)
    {
        bool _enabled = __instance.Vehicle.Company == __instance.Scene.Session.Player;

        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 13, 1, SizeType.Weight);
        _grid.Margin_local = new FloatSpace(MainData.Margin_content * 2, MainData.Margin_content_items);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.SetColumn(1, SizeType.Pixels, MainData.Margin_content);
        _grid.SetColumn(3, SizeType.Pixels, MainData.Margin_content);
        _grid.SetColumn(5, SizeType.Pixels, MainData.Margin_content);
        _grid.SetColumn(7, SizeType.Pixels, MainData.Margin_content);
        _grid.SetColumn(9, SizeType.Pixels, MainData.Margin_content);
        _grid.SetColumn(11, SizeType.Pixels, MainData.Margin_content);

        // 0 Route
        Button _route = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_locate, __instance.Scene.Engine).Control;
        _route.Enabled = _enabled;
        _grid.Transfer(_route, 0, 0);
        _route.OnMouseStillTime += () => GeneralTooltips.GetRoute(__instance.Scene.Engine).AddToControlAutoVertical(_route);
        _route.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenRoute", []);

        // 2 Edit
        Button _edit = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_cogwheel, __instance.Scene.Engine).Control;
        _edit.Enabled = _enabled;
        _grid.Transfer(_edit, 2, 0);
        _edit.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset tt = TooltipPreset.Get(Localization.GetVehicle("edit_route"), __instance.Scene.Engine);
            tt.AddDescription("Click to edit the route for all vehicles.");
            tt.AddDescription("Ctrl+Click to edit the vehicle's route and create a new one.");
            tt.AddToControlAutoVertical(_edit);
        };
        _edit.OnButtonPress += (Action)delegate
        {
            CreateNewRouteAction _action = new(__instance.Scene, __instance.Scene.Engine.Keys.Ctrl ? __instance.Vehicle : __instance.Vehicle.GetLine(__instance.Scene));
            __instance.Scene.SetOverrideAction(_action);
        };

        // 4 Change
        Button _change = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_fastest, __instance.Scene.Engine).Control;
        _change.Enabled = _enabled && __instance.Scene.Session.GetPlayer().Line_manager.Lines.Count > 0;
        _grid.Transfer(_change, 4, 0);
        _change.OnMouseStillTime += () => TooltipPreset.Get(Localization.GetVehicle("change_route"), __instance.Scene.Engine).AddToControlAutoVertical(_change);
        _change.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenChangeRouteOptions", [__instance.Main_control]);

        // 6 Replace
        Button _replace = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_replace, __instance.Scene.Engine).Control;
        _grid.Transfer(_replace, 6, 0);
        _replace.OnMouseStillTime += () => TooltipPreset.Get(Localization.GetGeneral("replace"), __instance.Scene.Engine).AddToControlAutoVertical(_replace);
        _replace.OnButtonPress += () => __instance.CallPrivateMethodVoid("GetUpgrade", [__instance.Main_control]);

        // 8 Hub
        Button _hub = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_storage, __instance.Scene.Engine).Control;
        _hub.Enabled = _enabled;
        _grid.Transfer(_hub, 8, 0);

        // 10 Duplicate
        Button _duplicate = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_duplicate, __instance.Scene.Engine).Control;
        _duplicate.Enabled = _enabled;
        _grid.Transfer(_duplicate, 10, 0);
        _duplicate.OnMouseStillTime += () => __instance.CallPrivateMethodVoid("GetDuplicateTooltip", [_duplicate]);
        _duplicate.OnButtonPress += () => __instance.CallPrivateMethodVoid("Duplicate", [_duplicate]);

        // 12 Sell
        Button _sell = ButtonPresets.IconClose(ContentRectangle.Stretched, MainData.Icon_trash, __instance.Scene.Engine).Control;
        _sell.Enabled = _enabled;
        _grid.Transfer(_sell, 12, 0);
        _sell.OnMouseStillTime += () => __instance.CallPrivateMethodVoid("GetSellTooltip", [_sell]);
        _sell.OnButtonPress += () =>
            ConfirmUI.Get(Localization.GetVehicle("sell_vehicle").Replace("{vehicle}", __instance.Vehicle.GetName()), null, delegate
            {
            }, () => __instance.CallPrivateMethodVoid("SellVehicle", []), __instance.Scene.Engine, null, null, -(long)((decimal)__instance.Vehicle.GetValue() * __instance.Scene.Session.GetPriceAdjust()));

        __instance.CallPrivateMethodVoid("AddControl", [_grid, "controls"]);

        // Determine current and a new hub
        ushort _company = __instance.Scene.Session.Player;
        List<Hub> _hubs = [];
        List<string> _hubNames = ["Hubs:"];
        for (int i = 0; i < __instance.Vehicle.Route.Instructions.Cities.Length; i++)
        {
            Hub _h = __instance.Vehicle.Route.Instructions.Cities[i].GetHub(_company);
            if (_h != null)
            {
                _hubs.Add(_h);
                _hubNames.Add(_h.GetName(__instance.Scene));
            }
        }

        // New open handler
        _hub.OnButtonPress += (Action)delegate
        {
            if (__instance.Scene.Engine.Keys.Ctrl)
            {
                if (_hubs.Count > 1)
                {
                    Hub _newHub = _hubs[(_hubs.IndexOf(__instance.Vehicle.Hub) + 1) % _hubs.Count];
                    __instance.Vehicle.ChangeHub(_newHub, __instance.Scene);
                    MainData.Sound_hub.Play();
                }
                else
                    MainData.Sound_error.Play();
            }
            else
                __instance.CallPrivateMethodVoid("OpenHub");
        };

        // New tooltip handler
        _hub.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset tt = GeneralTooltips.GetHub(__instance.Scene.Engine);
            tt.AddSeparator();
            tt.AddBoldLabel(__instance.Vehicle.Hub.GetNameWithIcon(__instance.Scene));
            if (_hubs.Count > 1)
            {
                tt.AddDescription(String.Join(" ", _hubNames));
                Hub _newHub = _hubs[(_hubs.IndexOf(__instance.Vehicle.Hub) + 1) % _hubs.Count];
                tt.AddDescription("Ctrl-Click to change hub to " + _newHub.GetNameWithIcon(__instance.Scene));
                if (_newHub.Full())
                    tt.AddStatsLine(
                        Localization.GetCompany("upgrade_hub"),
                        StrConversions.GetBalance(_newHub.GetNextLevelPrice(__instance.Scene.Session), __instance.Scene.currency));
            }
            else
                tt.AddDescription($"There is only {_hubs.Count} hub on the line.");
            tt.AddToControlAutoVertical(_hub);
        };

        return false;
    }


    public static void ChangeHub(this VehicleBaseUser vehicle, Hub newHub, GameScene scene)
    {
        long _price = newHub.Full() ? newHub.GetNextLevelPrice(scene.Session) : 0;
        Company _company = scene.Session.GetPlayer();
        if (!_company.Cheats && _company.Wealth < _price)
        {
            ConfirmUI.Get(Localization.GetInfrastructure("no_money"), null, null, delegate
            {
            }, scene.Engine, null, null, _price);
            MainData.Sound_error.Play();
            return;
        }
        if (newHub.Full())
            scene.Session.Commands.Add(new CommandUpgradeHub(_company.ID, newHub.City));
        vehicle.Hub.Vehicles.Remove(vehicle);
        vehicle.SetPublicProperty("Hub", newHub);
        newHub.Vehicles.Add(vehicle);
    }


    /// <summary>
    /// Returns the city name with country flag where the hub is located.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="scene"></param>
    /// <returns></returns>
    public static string GetNameWithIcon(this Hub hub, GameScene scene)
    {
        return scene.Cities[hub.City].User.GetNameWithIcon(scene);
    }

    public static string GetName(this Hub hub, GameScene scene)
    {
        return scene.Cities[hub.City].User.Name;
    }


    /// <summary>
    /// Find a named control in a floating window.
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="control"></param>
    /// <returns></returns>
    public static bool TryGetControl(this IFloatUI ui, string name, out IControl? control)
    {
        GrowArray<string> control_names = ui.GetPrivateField<GrowArray<string>>("control_names");
        ControlCollection main_collection = ui.GetPrivateField<ControlCollection>("main_collection");
        for (int i = 0; i < control_names.Count; i++)
            if (control_names[i] == name)
            {
                control = main_collection[i];
                return true;
            }
        control = null;
        return false;
    }


    /// <summary>
    /// Finds a hub on a line or returns vehicle's hub if it exists on the line.
    /// </summary>
    /// <param name="company"></param>
    /// <returns></returns>
    internal static Hub GetHubForVehicle(this Line line, ushort company, VehicleBaseUser vehicle)
    {
        foreach (CityUser city in line.Instructions.Cities)
            if (city.City.City_id == vehicle.Hub.City)
                return vehicle.Hub;
        return line.GetHubForVehicle(company);
    }


    [HarmonyPatch("ChangeRoute"), HarmonyPrefix]
    public static bool BaseVehicleUI_ChangeRoute_Prefix(BaseVehicleUI __instance, SimpleDropdownItem item)
    {
        __instance.Scene.UI.ClearNestedControls();
        Line _route = __instance.Scene.Session.GetPlayer().Line_manager.Lines[item.Id];
        Hub _hub = _route.GetHubForVehicle(__instance.Scene.Session.Player, __instance.Vehicle);
        if (_hub == null)
        {
            ConfirmUI.Get("There is no hub on the target route!", null, null, delegate { }, __instance.Scene.Engine);
            MainData.Sound_error.Play();
            return false;
        }
        // Calculate price
        long _price = (_hub.Full() && _hub != __instance.Vehicle.Hub ? _hub.GetNextLevelPrice(__instance.Scene.Session) : 0);
        if (__instance.Vehicle.Route.Moving)
            _price += __instance.Vehicle.Passengers.GetNextTripPrice(__instance.Vehicle.Route);
        long _import = __instance.Vehicle.GetImportCost(__instance.Scene.Cities[_hub.City].User, __instance.Scene);
        _price += _import;
        // Prepare a new route
        NewRouteSettings _settings = new NewRouteSettings(__instance.Vehicle);
        _settings.cyclic = _route.Instructions.Cyclic;
        _settings.Cities.Clear();
        _settings.Cities.Add(_route.Instructions.Cities);
        if (_price > __instance.Scene.Session.GetPlayer().Wealth)
        {
            ConfirmUI.Get(Localization.GetInfrastructure("no_money"), null, null, delegate { }, __instance.Scene.Engine, null, null, _price);
            MainData.Sound_error.Play();
            return false;
        }
        if (_hub.Full() && _hub != __instance.Vehicle.Hub)
            __instance.Scene.Session.Commands.Add(new CommandUpgradeHub(__instance.Scene.Session.Player, _hub.City));
        __instance.Scene.Session.Commands.Add(new CommandChangeRoute(__instance.Scene.Session.Player, _settings, __instance.Vehicle, __instance.Scene.Cities[_hub.City].User));
        MainData.Sound_buy.Play();
        return false;
    }


    [HarmonyPatch("GetItemTooltip"), HarmonyPrefix]
    public static bool BaseVehicleUI_GetItemTooltip(BaseVehicleUI __instance, SimpleDropdownItem item)
    {
        TooltipPreset _tooltip = TooltipPreset.Get(Localization.GetVehicle("change_route"), __instance.Scene.Engine);
        _tooltip.player_wealth = () => __instance.Scene.Session.GetPlayer().Wealth;
        Hub _hub = __instance.Scene.Session.GetPlayer().Line_manager.Lines[item.Id].GetHubForVehicle(__instance.Scene.Session.Player);
        if (_hub == null)
        {
            _tooltip.AddBoldLabel(Localization.GetCompany("no_hub")).Color = LabelPresets.Color_negative;
        }
        else
        {
            long _price = (_hub.Full() && _hub != __instance.Vehicle.Hub ? _hub.GetNextLevelPrice(__instance.Scene.Session) : 0);
            long _import = __instance.Vehicle.GetImportCost(__instance.Scene.Cities[_hub.City].User, __instance.Scene);
            _price += _import;
            RouteInstance _route = __instance.Vehicle.Route;
            _tooltip.AddPrice(() => _price + (_route.Loading ? 0 : __instance.Vehicle.Passengers.GetNextTripPrice(_route)), __instance.Scene.currency);
            _tooltip.AddPrice(() => _route.Moving ? __instance.Vehicle.Passengers.GetNextTripPrice(_route) : 0, __instance.Scene.currency, "<!cl:passengers:" + Localization.GetCity("passengers_refund") + ">", 1);
            if (_import > 0)
                _tooltip.AddPrice(_import, __instance.Scene.currency, "<!cl:import:" + Localization.GetGeneral("import") + ">", 1);
            if (_hub.Full() && _hub != __instance.Vehicle.Hub)
            {
                _tooltip.AddPrice(_hub.GetNextLevelPrice(__instance.Scene.Session), __instance.Scene.currency, "<!cl:hub:" + Localization.GetCompany("upgrade_hub") + ">", 1);
                ExplorerVehicleEntity.AppendPriceAdjust(_tooltip, __instance.Scene);
            }
        }
        _tooltip.AddToControlAuto(item.control);
        return false;
    }


    internal static (decimal, int, int) GetPassengerData(this VehiclePassengers passengers, CityUser from, CityUser to)
    {
        int _all = 0;
        decimal _sum = 0m;
        int _back = 0;
        for (int i = 0; i < passengers.Items.Count; i++)
            if (passengers.Items[i].Start.User == from && passengers.Items[i].Destination.User == to)
            {
                int people = passengers.Items[i].People;
                _all += people;
                _sum += passengers.Items[i].demand_price * (decimal)people;
                if (passengers.Items[i].going_back)
                    _back += people;
            }
        return (_all > 0 ? _sum / (decimal)_all : 0m, _all, _back);
    }

    internal static (decimal, decimal) GetDemandData(this VehiclePassengers passengers)
    {
        int _all = 0;
        decimal _sum = 0m;
        for (int i = 0; i < passengers.Items.Count; i++)
        {
            int people = passengers.Items[i].People;
            _all += people;
            _sum += passengers.Items[i].demand_price * (decimal)people;
        }
        return (_all > 0 ? _sum / (decimal)_all : 0m, _sum);
    }

    internal static readonly string Color_positive = $"<!#{LabelPresets.Color_positive.GetHex()}>";
    internal static readonly string Color_negative = $"<!#{LabelPresets.Color_negative.GetHex()}>";
    internal static readonly string Color_main = $"<!#{LabelPresets.Color_main.GetHex()}>";

    // PassengersItems is a private nested type, so it is very cumbersome to work with atm
    [HarmonyPatch("GetPassengersItem"), HarmonyPostfix]
    public static void GetPassengersItem(BaseVehicleUI __instance, object __result, object items, CityUser from, CityUser to)
    {
        // Process private class
        PropertyInfo prop = __result.GetType().GetProperty("Control", BindingFlags.Instance | BindingFlags.Public)!;
        Grid _grid = (Grid)prop.GetValue(__result)!;
        prop = __result.GetType().GetProperty("From", BindingFlags.Instance | BindingFlags.Public)!;
        CityUser _from = (CityUser)prop.GetValue(__result)!;
        prop = __result.GetType().GetProperty("To", BindingFlags.Instance | BindingFlags.Public)!;
        CityUser _to = (CityUser)prop.GetValue(__result)!;
        FieldInfo field = __result.GetType().GetField("count", BindingFlags.Instance | BindingFlags.Public)!;

        // Labels
        var _data = __instance.Vehicle.Passengers.GetPassengerData(_from, _to);
        string _fromTxt = from.GetNameWithIcon(__instance.Scene);
        if (_data.Item1 > 1.1m) _fromTxt += Color_positive;
        else if (_data.Item1 < 0.9m) _fromTxt += Color_negative;
        _fromTxt += $"   {_data.Item1:F2}";
        ((Label)_grid[3]).Text = _fromTxt;
        string _toTxt = to.GetNameWithIcon(__instance.Scene);
        if (_data.Item2 - _data.Item3 > 0) _toTxt += $"   <!cicon_passenger> {_data.Item2 - _data.Item3}";
        if (_data.Item3 > 0) _toTxt += $"   <!cicon_fastest> {_data.Item3}";
        ((Label)_grid[4]).Text = _toTxt;
    }

    [HarmonyPatch("GetCargo"), HarmonyPostfix]
    public static void BaseVehicleUI_GetCargo_Postfix(BaseVehicleUI __instance)
    {
        if (!__instance.TryGetControl("cargo", out IControl? control) || control == null)
        {
            Log.Write("Error. Failed to get the control grid.");
            return;
        }
        Grid _grid = (Grid)control;
        Label _passengers1 = (Label)_grid[2];
        Panel _fill = (Panel)_grid[4];
        ControlContainer _fill_container = (ControlContainer)_fill.Content;
        ControlCollection _fill_collection = (ControlCollection)_fill_container.Content;
        Label _passengers2 = (Label)_fill_collection[0];
        int maxCapacity = __instance.Vehicle.Entity_base is TrainEntity train ? train.Max_capacity : __instance.Vehicle.Entity_base.Capacity;
        decimal minCapacity = (decimal)__instance.Vehicle.Entity_base.Real_min_passengers;
        UpdatePassengers();
        _grid.OnUpdate += (Action)delegate
        {
            if (__instance.Vehicle.Route.Loading)
                UpdatePassengers();
        };
        // Helper
        void UpdatePassengers()
        {
            var data = __instance.Vehicle.Passengers.GetDemandData();
            string text = Localization.GetGeneral("passengers") + "   ";
            if (data.Item1 > 1.1m) text += Color_positive;
            else if (data.Item1 < 0.9m) text += Color_negative;
            else text += Color_main;
            text += $"{data.Item1:F2}   " + (data.Item2 < minCapacity ? Color_negative : Color_main ) + $"   {100f * (float)data.Item2 / (float)maxCapacity:F0}%";
            _passengers1.Text = text;
            _passengers2.Text = text;
        }
    }


    // Patch to add info about the route: length and travel time
    [HarmonyPatch("GetPerformance"), HarmonyPrefix]
    public static bool BaseVehicleUI_GetPerformance_Prefix(BaseVehicleUI __instance, ref Label ___label_balance, ref Label ___label_efficiency)
    {
        // Grid 7x6
        // Row 0,1: space0 + label1 + space2 + label345 + space6
        // Row 2: space
        // Row 3,4: icon + graph
        // Row 5: space
        int _height = MainData.Size_button * 4 + MainData.Margin_content_items * 2;
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, _height, 1f), 7, 6, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        //_grid.Margin_local = new FloatSpace(MainData.Margin_content, MainData.Margin_content_items);
        __instance.CallPrivateMethodVoid("AddControl", [_grid, "perf"]);
        _grid.SetColumn(0, SizeType.Pixels, MainData.Margin_content);
        _grid.SetColumn(2, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetColumn(6, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetRow(2, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetRow(5, SizeType.Pixels, MainData.Margin_content_items);

        // Panel
        Company _company = __instance.Vehicle.GetCompany(__instance.Scene);
        Panel _panel = new Panel(ContentRectangle.Stretched, MainData.Panel_company_back_bottom);
        _panel.Margin_local = new FloatSpace(-MainData.Margin_content + MainData.Margin_content_items + 2, 0f);
        _panel.Color = _company.Color_secondary;
        _panel.use_multi_texture = true;
        _grid.Transfer(_panel, 0, 4, _grid.Columns_count, 2);

        // Vehicle icon
        if (__instance.Vehicle.Entity_base.Graphics.Entity != null)
        {
            VehicleGraphicsSprite _graphics = __instance.Vehicle.Entity_base.Graphics.Entity.Icon;
            Panel _icon = new Panel(new VehicleSprite(_graphics, __instance.Scene.Session.Companies[__instance.Vehicle.Company].Info));
            _icon.horizontal_alignment = HorizontalAlignment.Stretch;
            _icon.vertical_alignment = VerticalAlignment.Stretch;
            _grid.Transfer(_icon, 1, 2, 1, 4);
            _icon.OnMouseStillTime += (Action)delegate
            {
                GeneralTooltips.GetVehicle(__instance.Vehicle.Entity_base, __instance.Scene.Engine).AddToControlAuto(_icon);
            };
        }

        // Performance tooltip
        ControlContainer _container_performance = new ControlContainer(ContentRectangle.Stretched);
        _container_performance.mouse_pass = false;
        _grid.Transfer(_container_performance, 0, 0, _grid.Columns_count, 2);
        _container_performance.OnMouseStillTime += () => __instance.CallPrivateMethodVoid("GetPerformanceTooltip", [_container_performance]);

        // Labels Row0
        Label _perf = LabelPresets.GetDefault(Localization.GetVehicle("performance"), __instance.Scene.Engine);
        _perf.horizontal_alignment = HorizontalAlignment.Left;
        _grid.Transfer(_perf, 1, 0);
        ___label_balance = LabelPresets.GetBold("", __instance.Scene.Engine);
        ___label_balance.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(___label_balance, 3, 0);
        ___label_efficiency = LabelPresets.GetBold("", __instance.Scene.Engine);
        ___label_efficiency.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(___label_efficiency, 4, 0);
        Label _passengers = LabelPresets.GetBold(StrConversions.CleanNumber(__instance.Vehicle.Throughput.GetQuarterAverage()) + " <!cicon_passenger><!cicon_fast>", __instance.Scene.Engine);
        _passengers.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(_passengers, 5, 0);

        // Labels Row1
        string cyclic = __instance.Vehicle.Route.Instructions.Cyclic ? " <!cicon_right><!cicon_right>" : " <!cicon_right><!cicon_left>";
        Label _route = LabelPresets.GetDefault(Localization.GetVehicle("route") + cyclic, __instance.Scene.Engine);
        _route.horizontal_alignment = HorizontalAlignment.Left;
        _grid.Transfer(_route, 1, 1);
        Label _dist = LabelPresets.GetBold(StrConversions.GetDistance(__instance.Vehicle.Route.Instructions.GetTotalDistance(Line.GetVehicleType(__instance.Vehicle))), __instance.Scene.Engine);
        _dist.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(_dist, 3, 1);
        Label _stops = LabelPresets.GetBold(__instance.Vehicle.Route.Instructions.Cities.Length.ToString() + " <!cicon_city>", __instance.Scene.Engine);
        _stops.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(_stops, 4, 1);
        Label _time = LabelPresets.GetBold($"{__instance.Vehicle.GetJourneyTime():F1}h", __instance.Scene.Engine);
        _time.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(_time, 5, 1);

        // Graph
        IControl _graph = ChartLine.GetSingle(
            new GraphSettings((x) => __instance.CallPrivateMethod<long>("GetValue", [x]), 12, -1L, -1L, 2L)
            { update_tooltip = (tooltip, settings, id) => __instance.CallPrivateMethodVoid("UpdateGraphTooltip", [tooltip, settings, id]) },
            fill: true, _company.GetGridColor());
        _grid.Transfer(_graph, 3, 3, 3, 2);

        return false;
    }


    // Calculates travel time in hours
    public static double GetJourneyTime(this VehicleBaseUser vehicle)
    {
        Route route = vehicle.Route.Instructions;
        if (route.Cities.Length < 2)
            return 0;

        double distance = route.GetTotalDistance(Line.GetVehicleType(vehicle));
        int waitStops = route.Cyclic ? route.Cities.Length : route.Cities.Length - 1;

        // Get station wait time
        int stationTime = vehicle.Type switch
        {
            UserTypes.Road_vehicle => MainData.Defaults.Bus_station_time,
            UserTypes.Train => MainData.Defaults.Train_station_time,
            UserTypes.Plane => MainData.Defaults.Plane_airport_time,
            UserTypes.Ship => MainData.Defaults.Ship_port_time,
            _ => 0,
        };
        return distance / (double)vehicle.Entity_base.Speed + (double)(waitStops * stationTime) / 3600.0;
    }
}
