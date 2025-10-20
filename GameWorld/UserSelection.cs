using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI.Floating;
using STMG.Utility;
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
                if (!___scene.Engine.Keys.Shift)
                {
                    CityUser city = (CityUser)__instance.Hover!;
                    if (city.Routes.Count > 0)
                        city.Routes[0].Vehicle.OpenRoute(___scene);
                }
                else
                    (__instance.Hover as CityUser)!.OpenHub(___scene);
            }
        }
    }

    public static void OpenRoute(this VehicleBaseUser vehicle, GameScene scene)
    {
        Line? _line = vehicle.GetLine(scene); //  scene.Session.Companies[vehicle.Company].Line_manager.GetLine(vehicle);
        if (_line != null && !scene.Selection.IsSelected(_line))
        {
            scene.Selection.AddUI(new RouteUI(_line, scene.Session.Companies[vehicle.Company], scene));
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
}
