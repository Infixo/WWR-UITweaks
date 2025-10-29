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
        _edit.OnMouseStillTime += () => TooltipPreset.Get(Localization.GetVehicle("edit_route"), __instance.Scene.Engine).AddToControlAutoVertical(_edit);
        _edit.OnButtonPress += () => __instance.CallPrivateMethodVoid("Edit", []);

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
}
