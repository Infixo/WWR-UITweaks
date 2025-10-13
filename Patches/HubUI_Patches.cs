using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.AI;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using Utilities;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(HubUI))]
public static class HubUI_Patches
{
    [HarmonyPatch("GetManager"), HarmonyPrefix]
    public static bool GetManager(HubUI __instance)
    {
        __instance.SetPrivateField("manager", __instance.Hub.Manager != null);
        if (__instance.Hub.Company != __instance.Scene.Session.Player)
        {
            return false;
        }
        if (__instance.Hub.Manager == null)
        {
            // 3 buttons instead of 1
            Grid gridButtons = new(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 7, 1, SizeType.Weight) // 3 buttons + 4 spaces
            {
                horizontal_alignment = HorizontalAlignment.Stretch,
                Margin_local = new FloatSpace(0f, MainData.Margin_content_items, MainData.Margin_content, MainData.Margin_content_items)
            };
            gridButtons.SetColumn(0, SizeType.Pixels, MainData.Margin_content);
            gridButtons.SetColumn(2, SizeType.Pixels, MainData.Margin_content_items);
            gridButtons.SetColumn(4, SizeType.Pixels, MainData.Margin_content_items);
            gridButtons.SetColumn(6, SizeType.Pixels, MainData.Margin_content_items);
            __instance.CallPrivateMethodVoid("AddControl", [gridButtons, "manager"]);

            // Hire default
            Button button1 = ButtonPresets.TextGreen(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), "<!cicon_passenger>", __instance.Scene.Engine).Control;
            button1.horizontal_alignment = HorizontalAlignment.Stretch;
            gridButtons.Transfer(button1, 1, 0);
            button1.OnMouseStillTime += new Action(() => __instance.GetHireManagerTooltipEx(button1)); // default
            button1.OnButtonPress += new Action(() => __instance.CallPrivateMethodVoid("HireManager", []));

            // Hire Country
            Button button2 = ButtonPresets.TextGreen(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), "<!cicon_passenger> <!cicon_country>", __instance.Scene.Engine).Control;
            button2.horizontal_alignment = HorizontalAlignment.Stretch;
            gridButtons.Transfer(button2, 3, 0);
            button2.OnMouseStillTime += new Action(() => __instance.GetHireManagerTooltipEx(button2, "(Country)"));
            button2.OnButtonPress += (Action)delegate
            {
                // store the setting into the preset default
                SavingStream _stream = new SavingStream();
                _stream += 0L; // budget Auto
                _stream += 0L; // payout Monthly
                _stream += false; // no roads
                _stream += false; // no rails
                _stream += true; // only manage
                _stream += true; // yes to all
                _stream += true;
                _stream += true;
                _stream += true;
                // find and store best brands
                Country country = __instance.City.City.GetCountry(__instance.Scene);
                var Road = country.GetBestVehicleCompany("road_vehicle", __instance.Scene);
                var Train = country.GetBestVehicleCompany("train", __instance.Scene);
                _stream += 2; // only brands for road vehicles and trains
                _stream += Road.Item1.Name;
                _stream += Train.Item1.Name;
                __instance.Scene.Session.GetPlayer().manager_default = _stream.GetBytesOrSelf();
                __instance.CallPrivateMethodVoid("HireManager", []);
            };

            // Hire Current
            Button button3 = ButtonPresets.TextGreen(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), "<!cicon_passenger> <!cicon_storage>", __instance.Scene.Engine).Control;
            button3.horizontal_alignment = HorizontalAlignment.Stretch;
            gridButtons.Transfer(button3, 5, 0);
            button3.OnMouseStillTime += new Action(() => __instance.GetHireManagerTooltipEx(button3, "(Current)"));
            button3.OnButtonPress += (Action)delegate
            {
                // Analyze current vehicles
                bool buy_road_vehicles = false;
                bool buy_trains = false;
                bool buy_planes = false;
                bool buy_ships = false;
                HashSet<string> brands = [];
                for (int i = 0; i < __instance.Hub.Vehicles.Count; i++)
                {
                    VehicleBaseUser vehicle = __instance.Hub.Vehicles[i];
                    switch (vehicle.Type)
                    {
                        case UserTypes.Road_vehicle: buy_road_vehicles = true; break;
                        case UserTypes.Train: buy_trains = true; break;
                        case UserTypes.Plane: buy_planes = true; break;
                        case UserTypes.Ship: buy_ships = true; break;
                    }
                    brands.Add(vehicle.Entity_base.Company.Entity.Name);
                }
                // store the setting into the preset default
                SavingStream _stream = new SavingStream();
                _stream += 0L; // budget Auto
                _stream += 0L; // payout Monthly
                _stream += false; // no roads
                _stream += false; // no rails
                _stream += true; // only manage
                _stream += buy_road_vehicles;
                _stream += buy_trains;
                _stream += buy_planes;
                _stream += buy_ships;
                // find and store best brands
                _stream += brands.Count;
                foreach (string brand in brands)
                    _stream += brand;
                __instance.Scene.Session.GetPlayer().manager_default = _stream.GetBytesOrSelf();
                __instance.CallPrivateMethodVoid("HireManager", []);
            };

            return false;
        }

        // Hub Manager
        __instance.SetPrivateField("manage", __instance.Hub.Manager.Only_manage);
        int _rows = 4;
        int _height = MainData.Size_button * _rows + MainData.Margin_content_items * (_rows - 1);
        _rows += _rows - 1;
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, _height, 1f), 1, _rows, SizeType.Weight);
        _grid.Margin_local = new FloatSpace(MainData.Margin_content, MainData.Margin_content_items);
        __instance.CallPrivateMethodVoid("AddControl", [_grid, "manager"]);
        for (int i = 0; i < _rows; i += 2)
        {
            _grid.SetRow(i, SizeType.Pixels, MainData.Size_button);
            if (i + 1 < _rows)
            {
                _grid.SetRow(i + 1, SizeType.Pixels, MainData.Margin_content_items);
            }
        }
        _rows = 0;
        object[] args = [_grid, _rows];
        __instance.CallPrivateMethodVoid("GetManagerHeader", args); // (_grid, ref _rows);
        __instance.CallPrivateMethodVoid("GetManagerTopRow", args); // (_grid, ref _rows);
        __instance.CallPrivateMethodVoid("GetManagerMiddleRow", args); // (_grid, ref _rows);
        __instance.CallPrivateMethodVoid("GetManagerBottomRow", args); // (_grid, ref _rows);

        return false;
    }


    public static void GetHireManagerTooltipEx(this HubUI hubui, IControl parent, string? version = null)
    {
        string header = Localization.GetCompany("hire_manager");
        if (version != null) header += " " + version;
        TooltipPreset tooltipPreset = TooltipPreset.Get(header, hubui.Scene.Engine);
        tooltipPreset.AddDescription(Localization.GetInfo("hub_manager"));
        tooltipPreset.AddSeparator();
        tooltipPreset.AddStatsLine(Localization.GetGeneral("taxes"), StrConversions.Percent((float)MainData.Defaults.Hub_manager_tax));
        tooltipPreset.player_wealth = () => hubui.Scene.Session.GetPlayer().Wealth;
        long _price = MainData.Defaults.Hub_manager_price;
        tooltipPreset.AddPrice(_price, hubui.Scene.currency);
        tooltipPreset.AddToControlAuto(parent);
    }


    [HarmonyPatch("GetBrandsTooltip"), HarmonyPrefix]
    public static bool HubUI_GetBrandsTooltip_Prefix(HubUI __instance, IControl parent)
    {
        TooltipPreset tooltipPreset = TooltipPreset.Get(Localization.GetCompany("manager_targets"), __instance.Scene.Engine);
        tooltipPreset.AddDescription(Localization.GetInfo("manager_targets"));
        // Generated plans
        tooltipPreset.AddSeparator();
        GrowArray<GeneratedPlan> generated = __instance.Hub.Manager.GetPrivateField<GrowArray<GeneratedPlan>>("generated");
        if (generated.Count > 0)
        {
            tooltipPreset.AddBoldLabel("Generated plans");
            for (int i = 0; i < generated.Count; i++)
                tooltipPreset.AddStatsLine($"{i}. {DecodeVehicle(generated[i].Settings.vehicle)} ({generated[i].age}m, {generated[i].Weight:F2})", StrConversions.GetBalance(generated[i].Price, __instance.Scene.currency));
        }
        else
            tooltipPreset.AddBoldLabel("No plans");
        // Attach
        tooltipPreset.AddToControlBellow(parent);
        return false;

        // Local helper
        string DecodeVehicle(int coded)
        {
            try
            {
                if (coded > 3000000) return "<!cicon_train>" + MainData.Trains[coded - 3000000].Translated_name;
                if (coded > 2000000) return "<!cicon_ship>" + MainData.Ships[coded - 2000000].Translated_name;
                if (coded > 1000000) return "<!cicon_road_vehicle>" + MainData.Road_vehicles[coded - 1000000].Translated_name;
                return "<!cicon_plane>" + MainData.Planes[coded].Translated_name;
            }
            catch
            {
                return "(error)"; // just a precaution for index of bounds
            }
        }
    }
}
