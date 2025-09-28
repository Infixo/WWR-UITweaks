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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static STM.UI.Floating.RouteUI;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(RouteUI))]
public static class RouteUI_Patches
{
    private VehicleItem Get(VehicleBaseUser vehicle)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].Vehicle == vehicle)
            {
                VehicleItem result = items[i];
                items[i] = null;
                return result;
            }
        }
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 7, 1, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content_items, MainData.Margin_content, MainData.Margin_content_items);
        _grid.Opacity = 0f;
        vehicles.Transfer(_grid);
        _grid.SetColumn(1, SizeType.Pixels, MainData.Margin_content_items);
        if (Company != Scene.Session.GetPlayer())
        {
            _grid.SetColumn(2, SizeType.Pixels, 0f);
            _grid.SetColumn(3, SizeType.Pixels, 0f);
            _grid.SetColumn(4, SizeType.Pixels, 0f);
            _grid.SetColumn(5, SizeType.Pixels, 0f);
            _grid.SetColumn(6, SizeType.Pixels, 0f);
        }
        else
        {
            _grid.SetColumn(2, SizeType.Pixels, MainData.Size_button);
            _grid.SetColumn(3, SizeType.Pixels, MainData.Margin_content_items);
            _grid.SetColumn(4, SizeType.Pixels, MainData.Size_button);
            _grid.SetColumn(5, SizeType.Pixels, MainData.Margin_content_items);
            _grid.SetColumn(6, SizeType.Pixels, MainData.Size_button);
        }
        ControlCollection _content;
        Button _button = ButtonPresets.GetBlack(ContentRectangle.Stretched, Scene.Engine, out _content);
        _grid.Transfer(_button, 0, 0);
        _button.OnButtonPress += (Action)delegate
        {
            vehicle.Select(Scene);
            Scene.tracking = vehicle;
        };
        _button.SetCloseAnimation(AnimationPresets.Opacity(0f, 0.2f));
        _button.OnMouseStillTime += (Action)delegate
        {
            HubUI.GetVehicleTooltip(vehicle, _button, Scene);
        };
        Label _name = LabelPresets.GetBold(vehicle.GetName(), Scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        _content.Transfer(_name);
        Label _balance = LabelPresets.GetBold("5", Scene.Engine);
        _balance.horizontal_alignment = HorizontalAlignment.Right;
        _balance.Margin_local = new FloatSpace(MainData.Margin_content);
        _balance.Opacity = 0.5f;
        _content.Transfer(_balance);
        _balance.OnUpdate += (Action)delegate
        {
            long currentMonth = vehicle.Balance.GetCurrentMonth();
            if (currentMonth < 0)
            {
                _balance.Color = LabelPresets.Color_negative;
            }
            else
            {
                _balance.Color = LabelPresets.Color_positive;
            }
            _balance.Text = StrConversions.GetBalance(currentMonth, Scene.currency);
        };
        if (Company != Scene.Session.GetPlayer())
        {
            return new VehicleItem(_grid, vehicle);
        }
        Button _upgrade = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_upgrade, Scene.Engine).Control;
        _grid.Transfer(_upgrade, 2, 0);
        _upgrade.OnButtonPress += (Action)delegate
        {
            GetUpgrade(base.Main_control, vehicle);
        };
        Button _duplicate = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_duplicate, Scene.Engine).Control;
        _duplicate.Enabled = vehicle.Company == Scene.Session.Player;
        _grid.Transfer(_duplicate, 4, 0);
        _duplicate.OnMouseStillTime += (Action)delegate
        {
            BaseVehicleUI.GetDuplicateTooltip(_duplicate, vehicle, Scene);
        };
        _duplicate.OnButtonPress += (Action)delegate
        {
            Duplicate(_duplicate, vehicle);
        };
        Button _sell = ButtonPresets.IconClose(ContentRectangle.Stretched, MainData.Icon_trash, Scene.Engine).Control;
        _grid.Transfer(_sell, 6, 0);
        _sell.OnMouseStillTime += (Action)delegate
        {
            BaseVehicleUI.GetSellTooltip(_sell, vehicle, Scene);
        };
        _sell.OnButtonPress += (Action)delegate
        {
            ConfirmUI.Get(Localization.GetVehicle("sell_vehicle").Replace("{vehicle}", vehicle.GetName()), null, delegate
            {
            }, delegate
            {
                BaseVehicleUI.SellVehicle(vehicle, Scene);
            }, Scene.Engine, null, null, -(long)((decimal)vehicle.GetValue() * Scene.Session.GetPriceAdjust()));
        };
        return new VehicleItem(_grid, vehicle);
    }
    private void GetUpgrade(IControl parent, VehicleBaseUser Vehicle)
    {
        UpgradeUI.Get(parent, Vehicle, Scene);
    }

    private void Duplicate(IControl parent, VehicleBaseUser Vehicle)
    {
        Vehicle.Duplicate(Scene);
        base.Main_control.Ui?.RemoveNestedControlsByParent(parent);
    }
}
