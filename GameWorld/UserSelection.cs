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
using Utilities;
using UITweaks.UIFloating;

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
                    ___scene.Selection.AddUI(new InterestUI((CityUser)__instance.Hover, ___scene));
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


    [HarmonyPatch(typeof(InfoUI), "GetInfo"), HarmonyPostfix]
    public static void InfoUI_GetInfo_Postfix(InfoUI __instance)
    {
        // panels [0] back [1] vehicles [2] info [3] logo
        Panel _panel = (Panel)((ControlCollection)__instance.Main_control).Items[2];
        ControlCollection _cc_panel = (ControlCollection)_panel.Content;

        const int CloseSize = 30; // width of the button

        // Move Hubs to the left and change hover panel
        Button _hubs = (Button)_cc_panel.Items[5];
        //ContentRectangle _hubsRect = _hubs.GetPrivateField<ContentRectangle>("rectangle"); // debug
        ContentRectangle _hubsRect = new ContentRectangle(717 - MainData.Size_button - CloseSize, 0f, MainData.Size_button, 0f, 1f);
        _hubsRect.vertical_alignment = VerticalAlignment.Stretch;
        _hubs.SetPrivateField("rectangle", _hubsRect);
        // Button -> Content -> Items[0] -> Graphics
        ControlCollection _cc_hubs = (ControlCollection)_hubs.Content;
        ((Panel)_cc_hubs.Items[0]).Graphics = MainData.Panel_info_vehicles_button_b;

        // New buttons
        Button _close = __instance.CallPrivateMethod<Button>("GetTopButton", [717 - CloseSize, CloseSize, MainData.Icon_close, "close", "Close all floaters", MainData.Panel_info_button_right]);
        _close.OnButtonPress += (Action)delegate
        {
            Session session = __instance.GetPrivateField<Session>("Session");
            session.Scene.Selection.RemoveAllUI();
        };
        _cc_panel.Transfer(_close);
    }


    internal static void RemoveAllUI(this UserSelection selection)
    {
        for (int i = selection.Floaters.Count - 1; i >= 0; i--)
            selection.CallPrivateMethodVoid("RemoveUI", [i]);
    }
}
