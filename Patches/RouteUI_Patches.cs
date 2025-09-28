using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.Engine;
using STMG.UI;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(RouteUI))]
public static class RouteUI_Patches
{
    [HarmonyPatch("Get"), HarmonyPrefix]
    public static bool RouteUI_Get_Prefix(RouteUI __instance, ref RouteUI.VehicleItem __result, VehicleBaseUser vehicle, RouteUI.VehicleItem[] ___items, ControlCollection ___vehicles, GrowArray<VehicleBaseUser> ___selected)
    {
        // caching
        for (int i = 0; i < ___items.Length; i++)
        {
            if (___items[i] != null && ___items[i].Vehicle == vehicle)
            {
                RouteUI.VehicleItem result = ___items[i];
                ___items[i] = null;
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
        //_grid.SetColumn(1, SizeType.Pixels, MainData.Margin_content_items);

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
            // vehicle
            _grid.SetColumn(3, SizeType.Pixels, MainData.Margin_content_items);
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
        Label _name = LabelPresets.GetDefault(vehicle.GetName(), __instance.Scene.Engine);
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

        // Upgrade
        Button _upgrade = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_upgrade, __instance.Scene.Engine).Control;
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

        // Delete / Sell
        Button _sell = ButtonPresets.IconClose(ContentRectangle.Stretched, MainData.Icon_trash, __instance.Scene.Engine).Control;
        _grid.Transfer(_sell, 8, 0);
        _sell.OnMouseStillTime += (Action)delegate
        {
            BaseVehicleUI.GetSellTooltip(_sell, vehicle, __instance.Scene);
        };
        _sell.OnButtonPress += (Action)delegate
        {
            ConfirmUI.Get(Localization.GetVehicle("sell_vehicle").Replace("{vehicle}", vehicle.GetName()), null, delegate
            {
            }, delegate
            {
                BaseVehicleUI.SellVehicle(vehicle, __instance.Scene);
            }, __instance.Scene.Engine, null, null, -(long)((decimal)vehicle.GetValue() * __instance.Scene.Session.GetPriceAdjust()));
        };

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
    /*
    private void GetUpgrade(IControl parent, VehicleBaseUser Vehicle)
    {
        UpgradeUI.Get(parent, Vehicle, Scene);
    }
    
    private void Duplicate(IControl parent, VehicleBaseUser Vehicle)
    {
        Vehicle.Duplicate(Scene);
        base.Main_control.Ui?.RemoveNestedControlsByParent(parent);
    }
    */
}
