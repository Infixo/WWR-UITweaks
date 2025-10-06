using HarmonyLib;
using Utilities;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerVehicleEntity))]
public static class ExplorerVehicleEntity_Patches
{
    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerVehicleEntity_GetMainControl_Prefix(ExplorerVehicleEntity __instance, GameScene scene, Company company, CityUser city)
    {
        // define more labels
        Label[] tmpLabels = new Label[11];
        ExtensionsHelper.SetPublicProperty(__instance, "Labels", tmpLabels);

        // control - button
        int _height = 32;
        Button main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, _height, 1f), scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: false, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        main_button.Opacity = 0f;
        main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        main_button.OnMouseStillTime += (Action)delegate
        {
            __instance.CallPrivateMethodVoid("GetTooltip", [scene]);
        };

        Image alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        alt.Opacity = 0f;
        _collection.Transfer(alt);

        // control - grid
        Grid main_grid = new Grid(ContentRectangle.Stretched, __instance.Labels.Length, 1, SizeType.Weight);
        main_grid.OnFirstUpdate += (Action)delegate
        {
            main_grid.update_children = false;
        };
        _collection.Transfer(main_grid);

        // Helper
        void InsertLabel(int at, Label label, HorizontalAlignment align = HorizontalAlignment.Center)
        {
            label.horizontal_alignment = align;
            label.Margin_local = new FloatSpace(MainData.Margin_content);
            main_grid.Transfer(label, at, 0);
            __instance.Labels[at] = label;
        }

        // 0 Name
        Label _name = LabelPresets.GetDefault(__instance.CallPrivateMethod<string>("GetName", []), scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_name, 0, 0);
        __instance.Labels[0] = _name;
        if (__instance.Locked)
        {
            __instance.Labels[0].Color = LabelPresets.Color_negative;
        }

        // 1 Company
        Label _company = LabelPresets.GetDefault(__instance.Entity.Company.Entity.Translated_name, scene.Engine);
        _company.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_company, 1, 0);
        __instance.Labels[1] = _company;

        // 2 Capacity
        //string _scapacity = ((!(__instance.Entity is TrainEntity _train)) ? StrConversions.CleanNumber(__instance.Entity.Capacity) : StrConversions.OutOf(__instance.Entity.Capacity, _train.Max_capacity));
        int capacity = ExtensionsHelper.GetPrivateField<int>(__instance, "capacity"); // we need it 2x
        Label _capacity = LabelPresets.GetDefault(StrConversions.CleanNumber(capacity), scene.Engine);
        InsertLabel(2, _capacity);

        // 3 min passengeres
        Label _minPass = LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.Entity.Real_min_passengers), scene.Engine);
        InsertLabel(3, _minPass);

        // 4 min%
        Label _minPerc = LabelPresets.GetDefault(StrConversions.Percent((float)__instance.Entity.Real_min_passengers / (float)capacity), scene.Engine);
        InsertLabel(4, _minPerc);

        // 3 Speed => 5
        Label _speed = LabelPresets.GetDefault(StrConversions.GetSpeed(__instance.Entity.Speed), scene.Engine);
        _speed.horizontal_alignment = HorizontalAlignment.Center;
        _speed.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_speed, 5, 0);
        __instance.Labels[5] = _speed;

        // 4 Inventory => 6
        Country country = ExtensionsHelper.GetPrivateField<Country>(__instance, "country");
        int stock = __instance.Entity.GetInventory(scene.Session.GetPlayer(), country, scene);
        int _add = __instance.Entity.Inventory + scene.Session.GetPlayer().Loyalty.GetAdditions(__instance.Entity);
        if (country == __instance.Entity.Company.Entity.Country.Item)
        {
            CountryBuff _buff = country.Buff;
            if (_buff != null && _buff.Buff == CountryBuff.BuffType.Local_stock)
            {
                _add = (int)((decimal)_add * (1m + _buff.Percent));
            }
        }
        Label _stock = LabelPresets.GetDefault(StrConversions.OutOf(stock, _add), scene.Engine);
        _stock.horizontal_alignment = HorizontalAlignment.Center;
        _stock.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_stock, 6, 0);
        __instance.Labels[6] = _stock;

        // 5 Price => 7
        long price = ExtensionsHelper.GetPrivateField<long>(__instance, "price");
        Label _price = LabelPresets.GetDefault(StrConversions.GetBalance(price, scene.currency), scene.Engine);
        _price.horizontal_alignment = HorizontalAlignment.Right;
        _price.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_price, 7, 0);
        __instance.Labels[7] = _price;

        // 8 Localization.GetVehicle("estimated_profit")
        //_tooltip.AddStatsLine("<!cl:estimated_profit:" + Localization.GetVehicle("estimated_profit") + ">", StrConversions.GetBalance(entity.GetEstimatedProfit() / 100000 * 100000, _scene.currency));
        Label _profit = LabelPresets.GetDefault(StrConversions.GetBalance(__instance.Entity.GetEstimatedProfit() / 100000 * 100000, scene.currency), scene.Engine);
        InsertLabel(8, _profit, HorizontalAlignment.Right);

        // 9 Throughput
        //float defDistance = 1000f;
        //float numTrips = __instance.Entity.Speed * 24 / defDistance;
        //float throughput = numTrips * (float)ExtensionsHelper.GetPrivateField<int>(__instance, "capacity");
        Label _through = LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.Entity.GetEstimatedThroughput()), scene.Engine);
        InsertLabel(9, _through);

        // 10 Range
        Label _range = LabelPresets.GetDefault("∞", scene.Engine);
        if (__instance.Entity is PlaneEntity _plane)
        {
            _range = LabelPresets.GetDefault(StrConversions.GetDistance(_plane.Range), scene.Engine);
            // TODO: make red if too far; range is not availble here
            //if (range > _plane.Range)
            //{
                //Labels[0].Color = (Labels[1].Color = (Labels[2].Color = (Labels[3].Color = LabelPresets.Color_negative)));
            //}
        }
        InsertLabel(10, _range);

        // store into private fields
        ExtensionsHelper.SetPrivateField(__instance, "main_grid", main_grid);
        ExtensionsHelper.SetPrivateField(__instance, "main_button", main_button);
        ExtensionsHelper.SetPrivateField(__instance, "alt", alt);
        ExtensionsHelper.SetPrivateField(__instance, "stock", stock);

        return false; // skip the original
    }


    /// <summary>
    /// Extension to calculate theoretical monthly throughput
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="scene"></param>
    /// <param name="company"></param>
    /// <returns></returns>
    public static int GetEstimatedThroughput(this VehicleBaseEntity vbe, float distance = 1000f)
    {
        int stationTime = 0; // in seconds
        switch (vbe.Type_name)
        {
            case "road_vehicle": stationTime = MainData.Defaults.Bus_station_time; break;
            case "train": stationTime = MainData.Defaults.Train_station_time; break;
            case "plane": stationTime = MainData.Defaults.Plane_airport_time; break;
            case "ship": stationTime = MainData.Defaults.Ship_port_time; break;
        }
        //Log.Write($"{vbe.ID } {vbe.Type_name} {vbe.Translated_name} speed={vbe.Speed} cap={vbe.Capacity} ticket={vbe.Passenger_pay_per_km} cost={vbe.Cost_per_km}");
        
        float numTrips = 24f / ( distance / (float)vbe.Speed + (float)stationTime / 3600f);
        float capacity = (vbe is TrainEntity entity) ? (float)entity.Max_capacity : (float)vbe.Capacity; //ExtensionsHelper.GetPrivateField<int>(vbe, "capacity");
        int throughput = (int)(numTrips * capacity);

        //Log.Write($"{vbe.ID } {vbe.Type_name} {vbe.Translated_name} speed={vbe.Speed} cap={capacity} time={stationTime} trips={numTrips} thr{throughput}");
        return throughput;
    }


    [HarmonyPatch("Update"), HarmonyPrefix]
    public static bool Update(ExplorerVehicleEntity __instance, GameScene scene, Company company)
    {
        // access private fields
        Country country = ExtensionsHelper.GetPrivateField<Country>(__instance, "country");
        int stock = __instance.Entity.GetInventory(scene.Session.GetPlayer(), country, scene);

        __instance.Labels[6].Color = ((stock == 0) ? LabelPresets.Color_negative : LabelPresets.Color_main); // inventory 4 => 6
        __instance.Labels[7].Color = ((ExtensionsHelper.GetPrivateField<long>(__instance, "price") > company.Wealth) ? LabelPresets.Color_negative : LabelPresets.Color_positive); // price 5 => 7
        if (scene.Settings.Game_mode == GameMode.Discover)
        {
            __instance.Labels[6].Text = "∞"; // 4 => 6
            return false; // skip original
        }
        int _add = __instance.Entity.Inventory + scene.Session.GetPlayer().Loyalty.GetAdditions(__instance.Entity);
        if (country == __instance.Entity.Company.Entity.Country.Item)
        {
            CountryBuff _buff = country.Buff;
            if (_buff != null && _buff.Buff == CountryBuff.BuffType.Local_stock)
            {
                _add = (int)((decimal)_add * (1m + _buff.Percent));
            }
        }
        __instance.Labels[6].Text = StrConversions.OutOf(stock, _add); // inventory 4 => 6
        
        ExtensionsHelper.SetPrivateField(__instance, "stock", stock); // store into the private field
        return false; // skip the original
    }


    [HarmonyPatch("Smaller"), HarmonyPrefix]
    // need to overrride entire method because there are new columns inserted in the middle
    public static bool ExplorerVehicleEntity_Smaller_Prefix(ExplorerVehicleEntity __instance, IExplorerItem item, int sort_id, ref bool __result)
    {
        ExplorerVehicleEntity _item = (ExplorerVehicleEntity)item;

        if (__instance.Valid != _item.Valid)
        {
            __result = __instance.Valid.CompareTo(_item.Valid) > 0;
            return false;
        }

        int sortColumn = sort_id % __instance.Labels.Length;
        int result = 0; // temporary comparison, for normal order result<0, for reversed order resut>0

        switch (sort_id % __instance.Labels.Length) // sort column
        {
            case 0: // Name
                result = __instance.Labels[0].Text.CompareTo(_item.Labels[0].Text);
                break;

            case 1: // Company
                result = __instance.Labels[1].Text.CompareTo(_item.Labels[1].Text);
                break;

            case 2: // Capacity
                result = ExtensionsHelper.GetPrivateField<int>(__instance, "capacity").CompareTo(ExtensionsHelper.GetPrivateField<int>(_item, "capacity"));
                break;

            case 3: // min passengers
                result = __instance.Entity.Real_min_passengers.CompareTo(_item.Entity.Real_min_passengers);
                break;

            case 4: // min%
                float minThis = (float)__instance.Entity.Real_min_passengers / (float)ExtensionsHelper.GetPrivateField<int>(__instance, "capacity");
                float minItem = (float)_item.Entity.Real_min_passengers / (float)ExtensionsHelper.GetPrivateField<int>(_item, "capacity");
                result = minThis.CompareTo(minItem);
                break;

            case 5: // Speed
                result = __instance.Entity.Speed.CompareTo(_item.Entity.Speed);
                break;

            case 6: // Inventory
                int stockThis = ExtensionsHelper.GetPrivateField<int>(__instance, "stock");
                int stockItem = ExtensionsHelper.GetPrivateField<int>(_item, "stock");
                result = stockThis.CompareTo(stockItem);
                break;

            case 7: // Price
                long priceThis = ExtensionsHelper.GetPrivateField<long>(__instance, "price");
                long priceItem = ExtensionsHelper.GetPrivateField<long>(_item, "price");
                result = priceThis.CompareTo(priceItem);
                break;

            case 8: // Profit
                result = __instance.Entity.GetEstimatedProfit().CompareTo(_item.Entity.GetEstimatedProfit());
                break;

            case 9: // Throughput
                result = __instance.Entity.GetEstimatedThroughput().CompareTo(_item.Entity.GetEstimatedThroughput());
                break;

            case 10: // Range 
                int rangeThis = __instance.Entity is PlaneEntity planeThis ? planeThis.Range : -1;
                int rangeItem = _item.Entity is PlaneEntity planeItem ? planeItem.Range : -1;
                if (rangeThis < 0 && rangeItem < 0) // go for backup
                    result = 0;
                else result = rangeItem.CompareTo(rangeThis); // reverse order by default!
                break;
        }

        // fallback for sorting - always by price, then by names if prices are the same
        if (result == 0)
        {
            long priceThis = ExtensionsHelper.GetPrivateField<long>(__instance, "price");
            long priceItem = ExtensionsHelper.GetPrivateField<long>(_item, "price");
            result = priceThis.CompareTo(priceItem);
            if (result == 0)
            {
                result = __instance.Labels[0].Text.CompareTo(item.Labels[0].Text);
            }
        }

        // Normal order: sortid < length, Reverse order: sortid >= length
        __result = (sort_id < __instance.Labels.Length) ? result < 0 : result > 0;

        return false; // skip the original
    }
}
