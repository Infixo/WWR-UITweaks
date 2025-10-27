using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Commands;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.UI.Control;
using STVisual.Utility;
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
        header.Text += " [" + ((vehicle.GetLine(scene)?.ID+1) ?? 0).ToString() + "]";
    }


    [HarmonyPatch("GetControls"), HarmonyPostfix]
    public static void BaseVehicleUI_GetControls_Postfix(BaseVehicleUI __instance)
    {
        if (!__instance.TryGetControl("controls", out IControl? control) || control == null)
        {
            Log.Write("Error. Failed to get control grid.");
            return;
        }
        Button _hub = (Button)((Grid)control)[3];

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
        _hub.OnButtonPress.Clear();
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
        _hub.OnMouseStillTime.Clear();
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
}
