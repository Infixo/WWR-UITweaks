using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.Engine;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using UITweaks.UI;
using UITweaks.UIExplorer;
using UITweaks.UIFloating;
using Utilities;

namespace UITweaks.GameWorld;


[HarmonyPatch(typeof(UserSelection))]
internal static class UserSelection_Patches
{
    [HarmonyPatch("Update"), HarmonyPostfix]
    internal static void UserSelection_Update_Postfix(UserSelection __instance, GameScene ___scene)
    {
        if (Hotkeys.engine.Mouse.right == KeyState.Pressed && __instance.Hover != null && ___scene.Action == null)
        {
            if (__instance.Hover is VehicleBaseUser)
            {
                if (!___scene.Engine.Keys.Shift)
                    (__instance.Hover as VehicleBaseUser)!.OpenRoute(___scene, false, false);
                else
                    ___scene.Cities[(__instance.Hover as VehicleBaseUser)!.Hub.City].User.OpenHub(___scene);
                return;
            }
            if (__instance.Hover is CityUser)
            {
                if (___scene.Engine.Keys.Ctrl)
                    InterestUI.OpenRegisterCity((CityUser)__instance.Hover, ___scene);
                else if (!___scene.Engine.Keys.Shift)
                {
                    GrowArray<VehicleBaseUser> vehicles = (__instance.Hover as CityUser)!.GetCityRoutesByVehicle();
                    for (int r = 0; r < vehicles.Count; r++)
                        vehicles[r].OpenRoute(___scene, true, true);
                }
                else
                    (__instance.Hover as CityUser)!.OpenHub(___scene);
            }
        }
    }

    public static void OpenRoute(this VehicleBaseUser vehicle, GameScene scene, bool pinned, bool check_type)
    {
        Line? _line = vehicle.GetLine(scene);
        if (_line == null) return;
        if (check_type)
            switch (_line.Vehicle_type)
            {
                case 0: if (!RoadToggle) return; break;
                case 1: if (!TrainToggle) return; break;
                case 2: if (!PlaneToggle) return; break;
                case 3: if (!ShipToggle) return; break;
            }
        if (!scene.Selection.IsSelected(_line))
        {
            RouteUI ui = new RouteUI(_line, scene.Session.Companies[vehicle.Company], scene);
            ui.SetPinned(pinned);
            scene.Selection.AddUI(ui);
        }
    }

    public static void OpenHub(this CityUser city, GameScene scene)
    {
        Hub _hub = city.GetHub(scene.Session.Player);
        if (_hub != null && !scene.Selection.IsSelected(_hub))
        {
            scene.Selection.AddUI(new HubUI(city, _hub, scene));
        }
    }


    // Based on STM.UI.Floating.CityUI.GetCityRoutes
    internal static GrowArray<VehicleBaseUser> GetCityRoutesByVehicle(this CityUser city)
    {
        GrowArray<VehicleBaseUser> _result = new GrowArray<VehicleBaseUser>();
        GrowArray<Route> _unique = new GrowArray<Route>(city.Routes.Count);
        for (int i = 0; i < city.Routes.Count; i++)
            if (_unique.AddSingle(city.Routes[i].Instructions))
                _result.Add(city.Routes[i].Vehicle);
        return _result;
    }


    private const int WealthGridSize = 420; // 717 - 4 * MainData.Size_button - 67 => 490
    private const int OneButtonSize = 52; // 190/4=47,5
    private const int NumExplorers = 6;
    private const int NumCloseButtons = 5;
    private const int LastButtonSize = 67;

    [HarmonyPatch(typeof(InfoUI), "GetMainControl"), HarmonyPrefix]
    public static bool GetMainControl(InfoUI __instance)
    {
        ControlCollection main_collection = new ControlCollection(new ContentRectangle(0f, 0f, (float)(123 + WealthGridSize + (NumExplorers+NumCloseButtons) * OneButtonSize + LastButtonSize), __instance.CallPrivateMethod<int>("GetHeight", []), 1f));
        __instance.SetPrivateField("main_collection", main_collection);
        main_collection.snaps_to_pixel = true;
        return false;
    }


    private static ButtonItem CityButton;
    private static ButtonItem RoadButton;
    private static ButtonItem TrainButton;
    private static ButtonItem ShipButton;
    private static ButtonItem PlaneButton;
    private static bool CityToggle = true;
    private static bool RoadToggle = true;
    private static bool TrainToggle = true;
    private static bool ShipToggle = true;
    private static bool PlaneToggle = true;


    [HarmonyPatch(typeof(InfoUI), "GetInfo"), HarmonyPrefix]
    public static bool InfoUI_GetInfo_Prefix(InfoUI __instance, ref Label ___balance, ref Label ___wealth, Session ___Session)
    {
        // Panel
        Panel _panel = new Panel(new ContentRectangle(123f, 0f, (float)(WealthGridSize + (NumExplorers+NumCloseButtons) * OneButtonSize + LastButtonSize), 51f, 1f), MainData.Panel_info_info);
        _panel.snaps_to_pixel = true;
        _panel.use_multi_texture = true;
        ((ControlCollection)__instance.Main_control).Transfer(_panel);
        ControlCollection _c = new ControlCollection(ContentRectangle.Stretched);
        _panel.TransferContent(_c);

        // Wealth grid
        Grid _wealth_grid = new Grid(ContentRectangle.Stretched, 2, 1, SizeType.Weight);
        _wealth_grid.mouse_pass = false;
        _wealth_grid.SetColumn(1, SizeType.Weight, 0.75f);
        ___wealth = LabelPresets.GetBold("", ___Session.Scene.Engine);
        ___wealth.horizontal_alignment = HorizontalAlignment.Right;
        _wealth_grid.Transfer(___wealth, 0, 0);
        ___balance = LabelPresets.GetBold("", ___Session.Scene.Engine);
        ___balance.Zoom_local = 0.8f;
        ___balance.Margin_local = new FloatSpace(MainData.Margin_content_items) / ___balance.Zoom_local;
        _wealth_grid.Transfer(___balance, 1, 0);

        // Wealth button
        Button _button = ButtonPresets.Get(ContentRectangle.Stretched, ___Session.Scene.Engine, out ControlCollection _collection, null, MainData.Panel_info_vehicles_button_b, mouse_pass: false, MainData.Sound_button_01_press, MainData.Sound_button_01_hover);
        _button.Margin_local = new FloatSpace((float)(NumExplorers * OneButtonSize), 0f, (float)(NumCloseButtons * OneButtonSize + LastButtonSize), 0f);
        _collection.Transfer(_wealth_grid);
        _c.Transfer(_button);
        _button.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl || ___Session.Scene.Engine.Keys.Shift)
                InfoUI_Patches.NoCapex = !InfoUI_Patches.NoCapex;
            else
                __instance.CallPrivateMethodVoid("OpenWealth", []);
        };
        _button.OnMouseStillTime += () => __instance.CallPrivateMethodVoid("GetWealthTooltip", [_button]);

        // Buttons
        int anchor = 0;

        // GetTopButton: x size icon tooltip header? hover?
        Button _companies = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_company, "company", "<!cicon_company> " + Localization.GetCompany("companies"), MainData.Panel_info_button_left]);
        _companies.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenCompanies", [_companies]);
        _c.Transfer(_companies);
        anchor += OneButtonSize;

        Button _countries = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_country, "country", "<!cicon_country> " + Localization.GetCity("countries"), null!]);
        _countries.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenCountries", [_countries]);
        _c.Transfer(_countries);
        anchor += OneButtonSize;

        Button _cities = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_city, "city", "<!cicon_city> " + Localization.GetCity("cities"), null!]);
        _cities.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenCities", [_cities]);
        _c.Transfer(_cities);
        anchor += OneButtonSize;

        Button _hubs = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_storage, "hub", "<!cicon_storage> " + Localization.GetCompany("hubs"), null!]);
        _hubs.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenHubs", [_hubs]);
        _c.Transfer(_hubs);
        anchor += OneButtonSize;

        Button _routes = __instance.CallPrivateMethod<Button>("GetTopButton", 
            [anchor, MainData.Size_button, MainData.Icon_locate, "route", "<!cicon_locate> " + Localization.GetVehicle("routes"), null!]);
        _c.Transfer(_routes);
        _routes.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenRoutes", [_routes]);
        anchor += OneButtonSize;

        Button _destinations = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_fastest, "destination", "Not connected destinations", null!]);
        _c.Transfer(_destinations);
        _destinations.OnButtonPress += () => ExplorerDestination.OpenExplorer(_destinations, ___Session);
        anchor += OneButtonSize;

        // Wealth grid
        anchor += WealthGridSize;

        // New buttons
        GameEngine engine = ___Session.Scene.Engine;
        CityButton = GetTopTextButton(ref anchor, "C", Localization.GetCity("cities") + " & " + Localization.GetCompany("hubs"), _c, engine);
        RoadButton = GetTopTextButton(ref anchor, "R", Localization.GetInfrastructure("road_vehicles"), _c, engine);
        TrainButton = GetTopTextButton(ref anchor, "T", Localization.GetInfrastructure("trains"), _c, engine);
        ShipButton = GetTopTextButton(ref anchor, "S", Localization.GetInfrastructure("ships"), _c, engine);
        PlaneButton = GetTopTextButton(ref anchor, "P", Localization.GetInfrastructure("planes"), _c, engine);
        bool temp = false;  UpdateButtons(ref temp);

        CityButton.Control.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl) UpdateButtons(ref CityToggle);
            else ___Session.Scene.Selection.RemoveCitiesUI();
        };

        RoadButton.Control.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl) UpdateButtons(ref RoadToggle);
            else ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Road_vehicle, 0);
        };

        TrainButton.Control.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl) UpdateButtons(ref TrainToggle);
            else ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Train, 1);
        };

        ShipButton.Control.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl) UpdateButtons(ref ShipToggle);
            else ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Ship, 3);
        };

        PlaneButton.Control.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl) UpdateButtons(ref PlaneToggle);
            else ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Plane, 2);
        };

        Button _close = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, LastButtonSize, MainData.Icon_close, "close", "Close all floaters", MainData.Panel_info_button_right]);
        _close.OnMouseStillTime.Clear();
        _close.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset tt = TooltipPreset.Get("<!cicon_close> " + Localization.GetGeneral("close"), engine);
            tt.AddDescription($"Click to close floaters of toggled types.");
            tt.AddDescription($"Ctrl-Click to close all floaters.");
            tt.AddToControlBellow(_close);
        };
        _close.OnButtonPress += (Action)delegate
        {
            if (___Session.Scene.Engine.Keys.Ctrl)
            {
                ___Session.Scene.Selection.RemoveAllUI();
                return;
            }
            if (CityToggle) ___Session.Scene.Selection.RemoveCitiesUI();
            if (RoadToggle) ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Road_vehicle, 0);
            if (TrainToggle) ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Train, 1);
            if (ShipToggle) ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Ship, 3);
            if (PlaneToggle) ___Session.Scene.Selection.RemoveVehiclesUI(UserTypes.Plane, 2);
        };
        _c.Transfer(_close);

        return false;

        // Helper \u2713 is check mark
        void UpdateButtons(ref bool toggle)
        {
            toggle = !toggle;
            CityButton.Label.Text = "<!cicon_city>" + (CityToggle ? $"{'\u2713'}" : $"{'\u2012'}");
            RoadButton.Label.Text = "<!cicon_road_vehicle>" + (RoadToggle ? $"{'\u2713'}" : $"{'\u2012'}");
            TrainButton.Label.Text = "<!cicon_train>" + (TrainToggle ? $"{'\u2713'}" : $"{'\u2012'}");
            ShipButton.Label.Text = "<!cicon_ship>" + (ShipToggle ? $"{'\u2713'}" : $"{'\u2012'}");
            PlaneButton.Label.Text = "<!cicon_plane>" + (PlaneToggle ? $"{'\u2713'}" : $"{'\u2012'}");
        }
    }


    internal static void RemoveAllUI(this UserSelection selection)
    {
        for (int i = selection.Floaters.Count - 1; i >= 0; i--)
            selection.CallPrivateMethodVoid("RemoveUI", [i]);
    }

    internal static void RemoveVehiclesUI(this UserSelection selection, UserTypes user_type, byte vehicle_type)
    {
        for (int i = selection.Floaters.Count - 1; i >= 0; i--)
            if ((selection.Floaters[i] is BaseVehicleUI _v && _v.User.Type == user_type) ||
                (selection.Floaters[i] is RouteUI _r) && _r.Line.Vehicle_type == vehicle_type)
                selection.CallPrivateMethodVoid("RemoveUI", [i]);
    }

    internal static void RemoveCitiesUI(this UserSelection selection)
    {
        for (int i = selection.Floaters.Count - 1; i >= 0; i--)
            if (selection.Floaters[i] is CityUI || selection.Floaters[i] is HubUI)
                selection.CallPrivateMethodVoid("RemoveUI", [i]);
    }


    internal static ButtonItem GetTopTextButton(ref int x, string text, string header, ControlCollection cc, GameEngine engine)
    {
        ButtonItem _buttonItem = ButtonPresets.Text(new ContentRectangle(x, 0f, OneButtonSize, 0f, 1f), text, engine, null, MainData.Panel_info_vehicles_button_b);
        x += OneButtonSize;
        _buttonItem.Control.vertical_alignment = VerticalAlignment.Stretch;
        _buttonItem.Control.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset tt = TooltipPreset.Get(header, engine);
            tt.AddDescription($"Click to close all {header} floaters.");
            tt.AddDescription($"Ctrl-Click to toggle mass closing and opening via Right-Click features.");
            tt.AddToControlBellow(_buttonItem.Control);
        };
        cc.Transfer(_buttonItem.Control);
        return _buttonItem;
    }
}
