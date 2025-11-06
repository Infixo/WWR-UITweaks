using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.Engine;
using STMG.UI;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
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
                    (__instance.Hover as VehicleBaseUser)!.OpenRoute(___scene);
                else
                    ___scene.Cities[(__instance.Hover as VehicleBaseUser)!.Hub.City].User.OpenHub(___scene);
                return;
            }
            if (__instance.Hover is CityUser)
            {
                if (___scene.Engine.Keys.Ctrl)
                    //___scene.Selection.AddUI(new InterestUI((CityUser)__instance.Hover, ___scene));
                    InterestUI.OpenRegisterCity((CityUser)__instance.Hover, ___scene);
                else if (!___scene.Engine.Keys.Shift)
                {
                    GrowArray<VehicleBaseUser> vehicles = (__instance.Hover as CityUser)!.GetCityRoutesByVehicle();
                    for (int r = 0; r < vehicles.Count; r++)
                        vehicles[r].OpenRoute(___scene, true);
                }
                else
                    (__instance.Hover as CityUser)!.OpenHub(___scene);
            }
        }
    }

    public static void OpenRoute(this VehicleBaseUser vehicle, GameScene scene, bool pinned = false)
    {
        Line? _line = vehicle.GetLine(scene); //  scene.Session.Companies[vehicle.Company].Line_manager.GetLine(vehicle);
        if (_line != null && !scene.Selection.IsSelected(_line))
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
    private const int OneButtonSize = 50; // 190/4=47,5
    private const int LastButtonSize = 67;

    [HarmonyPatch(typeof(InfoUI), "GetMainControl"), HarmonyPrefix]
    public static bool GetMainControl(InfoUI __instance)
    {
        ControlCollection main_collection = new ControlCollection(new ContentRectangle(0f, 0f, (float)(123 + WealthGridSize + 11 * OneButtonSize + LastButtonSize), __instance.CallPrivateMethod<int>("GetHeight", []), 1f));
        __instance.SetPrivateField("main_collection", main_collection);
        main_collection.snaps_to_pixel = true;
        return false;
    }


    private static ButtonItem CityButton;
    private static ButtonItem RoadButton;
    private static ButtonItem TrainButton;
    private static ButtonItem ShipButton;
    private static ButtonItem PlaneButton;
    private static ButtonItem AllButton;


    [HarmonyPatch(typeof(InfoUI), "GetInfo"), HarmonyPrefix]
    public static bool InfoUI_GetInfo_Prefix(InfoUI __instance, ref Label ___balance, ref Label ___wealth, Session ___Session)
    {
        // Panel
        Panel _panel = new Panel(new ContentRectangle(123f, 0f, (float)(WealthGridSize + 11 * OneButtonSize + LastButtonSize), 51f, 1f), MainData.Panel_info_info);
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
        _button.Margin_local = new FloatSpace((float)(5 * OneButtonSize), 0f, (float)(6 * OneButtonSize + LastButtonSize), 0f);
        _collection.Transfer(_wealth_grid);
        _c.Transfer(_button);
        _button.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenWealth", []);
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
            [anchor, MainData.Size_button, MainData.Icon_country, "country", "<!cicon_country> " + Localization.GetCity("countries"), null]);
        _countries.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenCountries", [_countries]);
        _c.Transfer(_countries);
        anchor += OneButtonSize;

        Button _cities = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_city, "city", "<!cicon_city> " + Localization.GetCity("cities"), null]);
        _cities.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenCities", [_cities]);
        _c.Transfer(_cities);
        anchor += OneButtonSize;

        Button _hubs = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, MainData.Size_button, MainData.Icon_storage, "hub", "<!cicon_storage> " + Localization.GetCompany("hubs"), null]);
        _hubs.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenHubs", [_hubs]);
        _c.Transfer(_hubs);
        anchor += OneButtonSize;

        Button _routes = __instance.CallPrivateMethod<Button>("GetTopButton", 
            [anchor, MainData.Size_button, MainData.Icon_locate, "route", "<!cicon_locate> " + Localization.GetVehicle("routes"), null]);
        _c.Transfer(_routes);
        _routes.OnButtonPress += () => __instance.CallPrivateMethodVoid("OpenRoutes", [_routes]);
        anchor += OneButtonSize;
        anchor += WealthGridSize;

        // New buttons
        GameEngine engine = ___Session.Scene.Engine;
        CityButton = GetTopTextButton(ref anchor, MainData.Size_button, "<!cicon_city>", "Cities and Hubs", _c, ___Session.Scene);
        RoadButton = GetTopTextButton(ref anchor, MainData.Size_button, "<!cicon_road_vehicle>", "Road vehicles", _c, ___Session.Scene);
        TrainButton = GetTopTextButton(ref anchor, MainData.Size_button, "<!cicon_train>", "Trains", _c, ___Session.Scene);
        ShipButton = GetTopTextButton(ref anchor, MainData.Size_button, "<!cicon_ship>", "Ships", _c, ___Session.Scene);
        PlaneButton = GetTopTextButton(ref anchor, MainData.Size_button, "<!cicon_plane>", "Planes", _c, ___Session.Scene);
        AllButton = GetTopTextButton(ref anchor, MainData.Size_button, "All", "All", _c, ___Session.Scene);

        Button _close = __instance.CallPrivateMethod<Button>("GetTopButton",
            [anchor, LastButtonSize, MainData.Icon_close, "close", "Close all floaters", MainData.Panel_info_button_right]);
        _close.OnButtonPress += (Action)delegate
        {
            Session session = __instance.GetPrivateField<Session>("Session");
            session.Scene.Selection.RemoveAllUI();
        };
        _c.Transfer(_close);

        return false;
    }


    internal static void RemoveAllUI(this UserSelection selection)
    {
        for (int i = selection.Floaters.Count - 1; i >= 0; i--)
            selection.CallPrivateMethodVoid("RemoveUI", [i]);
    }

    internal static void RemoveAllUI(this UserSelection selection, UserTypes type)
    {
        for (int i = selection.Floaters.Count - 1; i >= 0; i--)
            if (selection.Floaters[i].User.Type == type)
                selection.CallPrivateMethodVoid("RemoveUI", [i]);
    }


    internal static ButtonItem GetTopTextButton(ref int x, int size, string text, string tooltip, ControlCollection cc, GameScene scene)
    {
        ButtonItem _buttonItem = ButtonPresets.Text(new ContentRectangle(x, 0f, OneButtonSize, 0f, 1f), text, scene.Engine, null, MainData.Panel_info_vehicles_button_b); //, mouse_pass: false, MainData.Sound_button_01_press, MainData.Sound_button_01_hover);
        x += OneButtonSize;
        _buttonItem.Control.vertical_alignment = VerticalAlignment.Stretch;
        _buttonItem.Control.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset.Get(tooltip, scene.Engine).AddToControlBellow(_buttonItem.Control);
        };
        cc.Transfer(_buttonItem.Control);
        return _buttonItem;
    }
}
