using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Commands;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STM.UI.Stats;
using STMG.UI;
using STMG.UI.Control;
using STMG.UI.Utility;
using STVisual.Utility;
using System.Runtime.CompilerServices;
using Utilities;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(RouteUI))]
public static class RouteUI_Patches
{
    // Data extensions
    public class ExtraData
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Label Label_Throughput;
        public Label Label_Vehicles;
        public Button[] Buttons = new Button[4];
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
    private static readonly ConditionalWeakTable<RouteUI, ExtraData> _extras = [];
    public static ExtraData Extra(this RouteUI ui) => _extras.GetOrCreateValue(ui);


    [HarmonyPatch("Construct"), HarmonyPostfix]
    public static void RouteUI_Construct_Postfix(RouteUI __instance, Panel ___main_panel, int ___height)
    {
        // Make the window a bit wider to fit long ship names; Finalize sets it for hard 550
        ___main_panel.Size_local = new Microsoft.Xna.Framework.Vector2(600, ___height);
    }


    [HarmonyPatch("Get"), HarmonyPrefix]
    public static bool RouteUI_Get_Prefix(RouteUI __instance, ref RouteUI.VehicleItem __result, VehicleBaseUser vehicle, RouteUI.VehicleItem[] ___items, ControlCollection ___vehicles, GrowArray<VehicleBaseUser> ___selected)
    {
        // caching
        for (int i = 0; i < ___items.Length; i++)
        {
            if (___items[i] != null && ___items[i].Vehicle == vehicle)
            {
                RouteUI.VehicleItem result = ___items[i];
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                ___items[i] = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                __result = result;
                return false;
            }
        }

        // BUTTONS
        // original:         <name+balance><upgrade><duplicate><delete>  |    [   ][^][+][x]
        // current : <select><name+balance>                              | [.][   ]
        // modded  : <select><name+balance><upgrade><duplicate><promote> | [.][   ][=][+][^]

        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 9, 1, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content_items, MainData.Margin_content, MainData.Margin_content_items);
        _grid.Opacity = 0f;
        ___vehicles.Transfer(_grid);

        // Selection image
        Image _selection = new Image(ContentRectangle.Stretched, MainData.Panel_gradient_left);
        _selection.Margin_local = new FloatSpace(0f, 0f);
        _selection.Opacity = 0f;
        _selection.Color = LabelPresets.Color_main;
        _grid.Transfer(_selection, 0, 0, _grid.Columns_count, _grid.Rows_count);

        // Grid layout
        if (__instance.Company != __instance.Scene.Session.GetPlayer())
        {
            _grid.SetColumn(0, SizeType.Pixels, 0f);
            _grid.SetColumn(1, SizeType.Pixels, 0f);
            // vehicle
            _grid.SetColumn(3, SizeType.Pixels, 0f);
            _grid.SetColumn(4, SizeType.Pixels, 0f);
            _grid.SetColumn(5, SizeType.Pixels, 0f);
            _grid.SetColumn(6, SizeType.Pixels, 0f);
            _grid.SetColumn(7, SizeType.Pixels, 0f);
            _grid.SetColumn(8, SizeType.Pixels, 0f);
        }
        else
        {
            _grid.SetColumn(0, SizeType.Pixels, MainData.Size_button); // select
            _grid.SetColumn(1, SizeType.Pixels, MainData.Margin_content_items);
            //_grid.SetColumn(2, SizeType.Pixels, MainData.Size_button * 20); // vehicle
            _grid.SetColumn(3, SizeType.Pixels, MainData.Margin_content_items); // this is needed, otherwise the button is only half-size
            _grid.SetColumn(4, SizeType.Pixels, MainData.Size_button); // upgrade
            _grid.SetColumn(5, SizeType.Pixels, MainData.Margin_content_items);
            _grid.SetColumn(6, SizeType.Pixels, MainData.Size_button); // duplicate
            _grid.SetColumn(7, SizeType.Pixels, MainData.Margin_content_items);
            _grid.SetColumn(8, SizeType.Pixels, MainData.Size_button); // delete -> promote
        }

        // Vehicle
        ControlCollection _content;
        Button _button = ButtonPresets.GetBlack(ContentRectangle.Stretched, __instance.Scene.Engine, out _content);
        _grid.Transfer(_button, 2, 0);
        _button.OnButtonPress += (Action)delegate
        {
            vehicle.Select(__instance.Scene);
            __instance.Scene.tracking = vehicle;
        };
        _button.SetCloseAnimation(AnimationPresets.Opacity(0f, 0.2f));
        _button.OnMouseStillTime += (Action)delegate
        {
            HubUI.GetVehicleTooltip(vehicle, _button, __instance.Scene);
        };

        // Name
        Label _name = LabelPresets.GetDefault(vehicle.GetName() + (vehicle.Route.Moving ? " <!cicon_right>" : ""), __instance.Scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        _content.Transfer(_name);

        // Balance
        Label _balance = LabelPresets.GetDefault("5", __instance.Scene.Engine);
        _balance.horizontal_alignment = HorizontalAlignment.Right;
        _balance.Margin_local = new FloatSpace(MainData.Margin_content);
        _balance.Opacity = 0.5f;
        _content.Transfer(_balance);
        _balance.OnUpdate += (Action)delegate
        {
            long quarterAverage = vehicle.Balance.GetQuarterAverage();
            if (quarterAverage < 0)
            {
                _balance.Color = LabelPresets.Color_negative;
            }
            else
            {
                _balance.Color = LabelPresets.Color_positive;
            }
            _balance.Text = StrConversions.GetBalance(quarterAverage, __instance.Scene.currency);
        };

        // If non-player route, no actions are allowed
        if (__instance.Company != __instance.Scene.Session.GetPlayer())
        {
            __result = new RouteUI.VehicleItem(_grid, vehicle);
            return false;
        }

        // Upgrade / replace with any
        Button _upgrade = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_replace, __instance.Scene.Engine).Control;
        _upgrade.Enabled = vehicle.Company == __instance.Scene.Session.Player;
        _grid.Transfer(_upgrade, 4, 0);
        _upgrade.OnButtonPress += (Action)delegate
        {
            UpgradeUI.Get(__instance.Main_control, vehicle, __instance.Scene);
            //GetUpgrade(base.Main_control, vehicle);
        };

        // Duplicate
        Button _duplicate = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_duplicate, __instance.Scene.Engine).Control;
        _duplicate.Enabled = vehicle.Company == __instance.Scene.Session.Player;
        _grid.Transfer(_duplicate, 6, 0);
        _duplicate.OnMouseStillTime += (Action)delegate
        {
            BaseVehicleUI.GetDuplicateTooltip(_duplicate, [vehicle], __instance.Scene); // original took only 1 vehicle, now it is an array
        };
        _duplicate.OnButtonPress += (Action)delegate
        {
            vehicle.Duplicate(__instance.Scene);
            __instance.Main_control.Ui?.RemoveNestedControlsByParent(_duplicate);
            //Duplicate(_duplicate, vehicle);
        };

        // Upgrade to the next in chain
        Button _sell = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_upgrade, __instance.Scene.Engine).Control;
        _sell.Enabled = vehicle.Company == __instance.Scene.Session.Player;
        _grid.Transfer(_sell, 8, 0);
        _sell.OnMouseStillTime += (Action)delegate
        {
            BaseVehicleUI.GetSellTooltip(_sell, vehicle, __instance.Scene);
        };
        _sell.OnButtonPress += (Action)delegate
        {
            VehicleBaseEntity? entity = null;
            VehicleCompanyEntity vehicleCompany = vehicle.Entity_base.Company.Entity;
            for (int i = 0; i < vehicleCompany.Vehicles.Count; i++)
            {
                if (vehicleCompany.Vehicles[i].Price > vehicle.Entity_base.Price && vehicleCompany.Vehicles[i].CanBuy(__instance.Scene.Session.GetPlayer(), vehicle.Hub.Longitude))
                {
                    entity = vehicleCompany.Vehicles[i];
                    break;
                }
            }
            if (entity != null)
            {
                vehicle.UpgradeWith(entity, __instance.Scene);
                __instance.Main_control.Ui?.RemoveNestedControlsByParent(_sell);
            }
            else
                MainData.Sound_error.Play();
        };

        //foreach (VehicleBaseEntity item in filtered)
        //Log.Write($"{item.Tier} {item.Translated_name} from {item.Company.Entity.Translated_name} price: {item.Price}");

        // Select
        ButtonItem _select = ButtonPresets.IconBlack(ContentRectangle.Stretched, MainData.Icon_toggle_off, __instance.Scene.Engine);
        _grid.Transfer(_select.Control, 0, 0);
        if (___selected.Contains(vehicle))
        {
            _selection.Opacity = 1f;
            _select.Icon.Graphics = MainData.Icon_toggle_on;
        }
        _select.Control.OnUpdate += (Action)delegate
        {
            if (___selected.Count == 0 && _select.Icon.Graphics == MainData.Icon_toggle_on)
            {
                _select.Icon.Graphics = MainData.Icon_toggle_off;
                _selection.SetAnimation(AnimationPresets.Opacity(0f, 0.2f, Keyframe.EaseIn));
            }
        };
        _select.Control.OnButtonPress += (Action)delegate
        {
            if (___selected.Contains(vehicle))
            {
                ___selected.Remove(vehicle);
                _select.Icon.Graphics = MainData.Icon_toggle_off;
                _selection.SetAnimation(AnimationPresets.Opacity(0f, 0.2f, Keyframe.EaseIn));
            }
            else
            {
                ___selected.Add(vehicle);
                _select.Icon.Graphics = MainData.Icon_toggle_on;
                _selection.SetAnimation(AnimationPresets.Opacity(1f, 0.2f, Keyframe.EaseOut));
            }
            __instance.CallPrivateMethodVoid("SaveSelected", []);
        };

        // Final result
        __result = new RouteUI.VehicleItem(_grid, vehicle);
        return false;
    }


    [HarmonyPatch("GetRoute"), HarmonyPrefix]
    public static bool RouteUI_GetRoute_Prefix(RouteUI __instance)
    {
        __instance.SetPrivateField("route", __instance.Line.Instructions.Cities);
        int _height = MainData.Size_button * __instance.Line.Instructions.Cities.Length;
        //if (!Line.Instructions.Cyclic)
        //{
            //_height += MainData.Size_button * (Line.Instructions.Cities.Length - 2);
        //}
        int _height_max = _height;
        int _limit = (int)(__instance.Scene.UI.controls.Size_local.Y - 1200f);
        if (_limit > 600)
        {
            _limit = 600;
        }
        else if (_limit < 200)
        {
            _limit = 200;
        }
        if (_height > _limit)
        {
            _height = _limit;
        }
        ControlCollection _route = new ControlCollection(new ContentRectangle(0f, 0f, 0f, _height_max, 1f));
        _route.horizontal_alignment = HorizontalAlignment.Stretch;
        __instance.RouteUI_PopulateRoute_Prefix(_route);
        if (_height_max == _height)
        {
            _route.Margin_local = new FloatSpace(MainData.Margin_content, 0f);
            __instance.CallPrivateMethodVoid("AddControl", [_route, "route"]);
            return false;
        }
        IControl _scroll = ScrollPreset.GetVertical(new ContentRectangle(0f, 0f, 0f, _height, 1f), _route, ContentPreset.GetScrollSettings());
        _scroll.horizontal_alignment = HorizontalAlignment.Stretch;
        _scroll.Margin_local = new FloatSpace(MainData.Margin_content, 0f);
        __instance.CallPrivateMethodVoid("AddControl", [_scroll, "route"]);
        return false;
    }

    public static Dictionary<ushort, int> Hubs = []; // cities with hubs for this line

    //[HarmonyPatch("PopulateRoute"), HarmonyPrefix]
    public static bool RouteUI_PopulateRoute_Prefix(this RouteUI __instance, ControlCollection route)
    {
        // collect hub cities
        Hubs = [];
        for (int i = 0; i < __instance.Line.Routes.Count; i++)
        {
            VehicleBaseUser veh = __instance.Line.Routes[i].Vehicle;
            Hubs.TryAdd(veh.Hub.City, 0);
            Hubs[veh.Hub.City]++;
        }
        //RouteCycle _cycle = default(RouteCycle);
        float _y = 0f;
        for (int i = 0; i< __instance.Line.Instructions.Cities.Length; i++)
            __instance.RouteUI_GetRouteControl_Prefix(route, i, ref _y);
        //_cycle.Move(Line.Instructions);
        //while (_cycle.Current != 0)
        //{
            //GetRouteControl(route, _cycle, ref _y);
            //_cycle.Move(Line.Instructions);
        //}
        return false;
    }


    //[HarmonyPatch("GetRouteControl"), HarmonyPrefix]
    public static bool RouteUI_GetRouteControl_Prefix(this RouteUI __instance, ControlCollection route, int index, ref float y)
    {
        CityUser _city = __instance.Line.Instructions.Cities[index];

        // Button
        ControlCollection _content;
        Button _button = ButtonPresets.GetBlack(new ContentRectangle(0f, y, 0f, MainData.Size_button, 1f), __instance.Scene.Engine, out _content);
        _button.horizontal_alignment = HorizontalAlignment.Stretch;
        y += _button.Size_local_total.Y;
        route.Transfer(_button);
        _button.OnMouseStillTime += (Action)delegate
        {
            GeneralTooltips.GetCity(__instance.Scene.Engine, _city).AddToControlAuto(_button);
        };
        _button.OnButtonPress += (Action)delegate
        {
            if (__instance.Scene.tracking == _city)
            {
                _city.Select(__instance.Scene, track: true);
            }
            else
            {
                __instance.Scene.tracking = _city;
            }
        };

        // Grid - inside of the Button
        Grid _grid = new Grid(ContentRectangle.Stretched, 4, 1, SizeType.Weight);
        _grid.SetColumn(0, SizeType.Pixels, MainData.Size_button);
        _grid.SetColumn(3, SizeType.Pixels, MainData.Size_button/2);
        _content.Transfer(_grid);

        // 0 Id
        Label _id = LabelPresets.GetBold(StrConversions.CleanNumber(index + 1) + ".", __instance.Scene.Engine);
        _id.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(_id, 0, 0);

        // 1 Name + overcrowded color
        string name = _city.GetNameWithIcon(__instance.Scene);
        ushort player = __instance.Scene.Session.Player;
        if (_city.GetHub(player) != null && Hubs.ContainsKey(_city.GetHub(player).City)) name += " <!cicon_storage>";
        Label _city_label = LabelPresets.GetBold(name, __instance.Scene.Engine);
        _city_label.Color = _city.OvercrowdedColor(LabelPresets.Color_main);
        _grid.Transfer(_city_label, 1, 0);

        // 2 Waiting
        Label _waiting = LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.Line.GetWaiting(_city)), __instance.Scene.Engine);
        _waiting.horizontal_alignment = HorizontalAlignment.Right;
        _grid.Transfer(_waiting, 2, 0);

        // 3 Empty, place for scroll

        return false;
    }


    [HarmonyPatch("GetPerformance"), HarmonyPrefix]
    public static bool RouteUI_GetPerformance_Prefix(RouteUI __instance, ref Label ___label_balance, ref Label ___label_efficiency)
    {
        // Grid 1x11
        // 0: eff + bal
        // 1: space
        // 2: thr + veh
        // 3: space
        // 4: wait + dist
        // 5: space
        // 6-9: graph (4 rows)
        // 10: space
        int _height = MainData.Size_button * 7 + MainData.Margin_content_items * 4;
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, _height, 1f), 1, 11, SizeType.Weight);
        _grid.Margin_local = new FloatSpace(MainData.Margin_content, MainData.Margin_content_items);
        __instance.CallPrivateMethodVoid("AddControl", [_grid, "perf"]);
        _grid.SetRow( 1, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetRow( 3, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetRow( 5, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetRow(10, SizeType.Pixels, MainData.Margin_content_items);

        // Panel        
        Panel _panel = new Panel(ContentRectangle.Stretched, MainData.Panel_company_back_bottom);
        _panel.Margin_local = new FloatSpace(-MainData.Margin_content + MainData.Margin_content_items + 2, 0f);
        _panel.Color = __instance.Company.Color_secondary;
        _panel.use_multi_texture = true;
        _grid.Transfer(_panel, 0, 4, _grid.Columns_count, _grid.Rows_count - 4);

        // Performance tooltip
        ControlContainer _container_performance = new ControlContainer(ContentRectangle.Stretched);
        _container_performance.mouse_pass = false;
        _grid.Transfer(_container_performance, 0, 0);
        _container_performance.OnMouseStillTime += () => __instance.CallPrivateMethodVoid("GetPerformanceTooltip", [_container_performance]);

        // Evaluation tooltip
        if (AITweaksLink.Active)
        {
            ControlContainer _container_evaluation = new ControlContainer(ContentRectangle.Stretched);
            _container_evaluation.mouse_pass = false;
            _grid.Transfer(_container_evaluation, 0, 2, row_span: 3);
            _container_evaluation.OnMouseStillTime += () => AITweaksLink.GetEvaluationTooltip(__instance.Line, __instance.Scene.Engine).AddToControlAuto(_container_evaluation);
        }

        // Row0 Balance
        ___label_balance = LabelPresets.GetBold("", __instance.Scene.Engine);
        ___label_balance.horizontal_alignment = HorizontalAlignment.Right;
        ___label_balance.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(___label_balance, 0, 0);

        // Row0 Efficiency
        ___label_efficiency = LabelPresets.GetBold("", __instance.Scene.Engine);
        ___label_efficiency.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(___label_efficiency, 0, 0);

        // Row1 Throughtput
        Label throughput = LabelPresets.GetBold(StrConversions.CleanNumber(__instance.Line.GetQuarterAverageThroughput()) + " <!cicon_fast>", __instance.Scene.Engine);
        throughput.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(throughput, 0, 2);
        __instance.Extra().Label_Throughput = throughput;

        // Row1 Vehicles
        __instance.Extra().Label_Vehicles = LabelPresets.GetBold("", __instance.Scene.Engine);
        __instance.Extra().Label_Vehicles.Margin_local = new FloatSpace(MainData.Margin_content);
        __instance.Extra().Label_Vehicles.horizontal_alignment = HorizontalAlignment.Right;
        _grid.Transfer(__instance.Extra().Label_Vehicles, 0, 2);

        // Row2 Waiting
        Label waiting = LabelPresets.GetBold(StrConversions.CleanNumber(__instance.Line.GetWaiting())+" <!cicon_passenger>", __instance.Scene.Engine);
        waiting.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(waiting, 0, 4);

        // Row2 Distance
        string cyclic = Localization.GetInfrastructure(__instance.Line.Instructions.Cyclic ? "route_cyclic_yes" : "route_cyclic_no");
        Label distance = LabelPresets.GetBold(StrConversions.GetDistance(__instance.Line.GetTotalDistance()) + $" ({cyclic})", __instance.Scene.Engine);
        distance.Margin_local = new FloatSpace(MainData.Margin_content);
        distance.horizontal_alignment = HorizontalAlignment.Right;
        _grid.Transfer(distance, 0, 4);

        // Balance graph
        IControl _graph = ChartLine.GetSingle(new GraphSettings(step => __instance.CallPrivateMethod<long>("GetValue", [step]), 12, -1L, -1L, 2L)
        {
            // lamba magic here!
            update_tooltip = (tooltip, settings, id) => __instance.CallPrivateMethodVoid("UpdateGraphTooltip", [tooltip, settings, id])
        }, fill: true, __instance.Company.GetGridColor());
        _grid.Transfer(_graph, 0, 6, 1, 4);
        _graph.Margin_local = new FloatSpace(MainData.Margin_content);

        return false;
    }


    [HarmonyPatch("Update"), HarmonyPostfix]
    public static void RouteUI_Update_Postfix(RouteUI __instance)
    {
        if (__instance.Main_control.Closing || __instance.Main_control.Ui == null)
            return;
        // path arrow updated
        // company info updated
        // Balance, efficieny and vehicles updated
        // Throughput
        __instance.Extra().Label_Throughput.Text = StrConversions.CleanNumber(__instance.Line.GetQuarterAverageThroughput()) + " <!cicon_fast>";
        // Vehicles
        string typeIcon = "?";
        switch (__instance.Line.Vehicle_type)
        {
            case 0: typeIcon = " <!cicon_road_vehicle>"; break;
            case 1: typeIcon = " <!cicon_train>"; break;
            case 2: typeIcon = " <!cicon_plane>"; break;
            case 3: typeIcon = " <!cicon_ship>"; break;
        }
        __instance.Extra().Label_Vehicles.Text = StrConversions.CleanNumber(__instance.Line.Vehicles) + typeIcon;
        // Buttons
        if (__instance.Company == __instance.Scene.Session.GetPlayer())
            __instance.UpdateButtons();
    }


    // Calculate actual efficieny (it is NOT average of efficiencies!)
    [HarmonyPatch(typeof(Line), "GetQuarterAverageEfficiency"), HarmonyPrefix]
    public static bool Line_GetQuarterAverageEfficiency_Prefix(Line __instance, ref long __result)
    {
        if (__instance.Vehicles == 0)
        {
            __result = 0;
            return false;
        }
        long transported = 0; // how many we actually transported
        long maxCapacity = 0; // how many we could
        for (int i = 0; i < __instance.Routes.Count; i++)
        {
            VehicleBaseUser vehicle = __instance.Routes[i].Vehicle;
            long throughput = vehicle.Throughput.GetQuarterAverage();
            long efficiency = vehicle.Efficiency.GetQuarterAverage();
            if (efficiency > 0)
            //for (int offset = 0; offset < 3; offset++)
                //if (vehicle.Throughput.Months > offset && vehicle.Efficiency.GetOffset(offset) > 0)
            {
                transported += throughput;
                maxCapacity += throughput * 100 / efficiency;
            }
        }
        __result = maxCapacity > 0 ? transported * 100 / maxCapacity : 0;
        return false;
    }


    public static void UpgradeWith(this VehicleBaseUser vehicle, VehicleBaseEntity entity, GameScene scene)
    {
        Company _company = scene.Session.GetPlayer();
        if (!_company.Cheats && _company.GetInventory(entity, scene.Cities[vehicle.Hub.City].GetCountry(scene), scene) == 0)
        {
            VehicleBaseUser.GetDuplicateError(scene);
            MainData.Sound_error.Play();
            return;
        }
        long _price = entity.GetPrice(scene, _company, scene.Cities[vehicle.Hub.City].User); // buy new
        _price -= (long)((decimal)vehicle.GetValue() * scene.Session.GetPriceAdjust()); // sell old
        if (vehicle.Hub.Full())
        {
            _price += vehicle.Hub.GetNextLevelPrice(scene.Session);
        }
        if (!_company.Cheats && _company.Wealth < _price)
        {
            ConfirmUI.Get(Localization.GetInfrastructure("no_money"), null, null, delegate
            {
            }, scene.Engine, null, null, _price);
            MainData.Sound_error.Play();
            return;
        }
        // schedule commands
        if (vehicle.Hub.Full())
        {
            scene.Session.Commands.Add(new CommandUpgradeHub(_company.ID, vehicle.Hub.City));
        }
        scene.Session.Commands.Add(new CommandSell(vehicle));
        NewRouteSettings _settings = new NewRouteSettings(vehicle);
        _settings.upgrade = new UpgradeSettings(vehicle, scene);
        _settings.SetVehicleEntity(entity);
        CommandNewRoute _command = new CommandNewRoute(scene.Session.Player, _settings, open: true);
        scene.Session.Commands.Add(_command);
        MainData.Sound_buy.Play();
    }


    [HarmonyPatch("GetRouteEdit"), HarmonyPrefix]
    public static bool RouteUI_GetRouteEdit_Prefix(RouteUI __instance)
    {
        if (__instance.Company != __instance.Scene.Session.GetPlayer())
            return false;

        // Grid
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 8, 1, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(MainData.Margin_content_items, MainData.Margin_content_items);
        __instance.CallPrivateMethodVoid("AddControl", [_grid, "edit"]);

        // Grid layout
        _grid.SetColumn(0, SizeType.Pixels, MainData.Size_button); // space
        _grid.SetColumn(2, SizeType.Pixels, MainData.Size_button); // space
        _grid.SetColumn(3, SizeType.Pixels, MainData.Size_button); // Road
        _grid.SetColumn(4, SizeType.Pixels, MainData.Size_button); // Train
        _grid.SetColumn(5, SizeType.Pixels, MainData.Size_button); // Plane
        _grid.SetColumn(6, SizeType.Pixels, MainData.Size_button); // Ship
        _grid.SetColumn(7, SizeType.Pixels, MainData.Size_button); // space

        // Edit route button
        Button _button = ButtonPresets.TextBlack(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), Localization.GetVehicle("edit_route"), __instance.Scene.Engine).Control;
        _button.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Transfer(_button, 1, 0);
        _button.OnButtonPress += () => __instance.CallPrivateMethodVoid("Edit", []);

        // Buttons for vehicle types
        for (int vt = 0; vt < 4; vt++)
        {
            Button button = ButtonPresets.TextGeneral(new ContentRectangle(0f, 0f, MainData.Size_button, MainData.Size_button, 1f), WorldwideRushExtensions.GetVehicleTypeIcon(vt), __instance.Scene.Engine).Control;
            _grid.Transfer(button, vt + 3, 0);
            byte vehicleType = (byte)vt;
            button.OnButtonPress += (Action)delegate
            {
                __instance.Line.SetPrivateField("vehicle_type", vehicleType);
                __instance.GetPrivateProperty<LongText>("Label_header").Text = __instance.Line.GetName();
            };
            __instance.Extra().Buttons[vt] = button;
        }
        __instance.UpdateButtons();

        return false;
    }


    internal static void UpdateButtons(this RouteUI ui)
    {
        for (int vt = 0; vt < 4; vt++)
            ui.Extra().Buttons[vt].Enabled = (ui.Line.Vehicles == 0 && ui.Line.Vehicle_type != vt);
    }
}
