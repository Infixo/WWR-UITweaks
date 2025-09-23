using HarmonyLib;
using Microsoft.Xna.Framework;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.Engine;
using STMG.UI.Control;
using STVisual.Utility;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerVehicleEntity))]
public static class ExplorerVehicleEntity_Patches
{
    /* not needed
     * 
    [HarmonyPatch(
        typeof(ExplorerVehicleEntity),
        MethodType.Constructor,
        [typeof(VehicleBaseEntity), typeof(GameScene), typeof(Company), typeof(CityUser), typeof(long), typeof(int)])]
    [HarmonyPostfix]
    public static void ExplorerVehicleEntity_Postfix(ExplorerVehicleEntity __instance, VehicleBaseEntity entity, GameScene scene, Company company, CityUser city, long price_adjust, int range = 0)
    {
        Log.Write($"{entity.Translated_name} comp={company.ID} inf={price_adjust} ran={range} {__instance.Labels[8].Text} {__instance.Labels[9].Text}");
        //Log.WriteCallingStack(10);
        //return true; // continue
        // MODDED
        //__instance.Labels[6].Text = "profit";
        //__instance.Labels[7].Text = "effic";
        //__instance.Labels[8].Text = "through";
        //__instance.Labels[9].Text = "range";
    }
    */


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
            ExplorerVehicleEntity_GetTooltip_Reverse(__instance, scene);
        };

        Image alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        alt.Opacity = 0f;
        _collection.Transfer(alt);

        // control - grid
        Grid main_grid = new Grid(ContentRectangle.Stretched, __instance.Labels.Length, 1, SizeType.Weight);
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
        Label _name = LabelPresets.GetDefault(ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetName", []), scene.Engine);
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
        Label _capacity = LabelPresets.GetDefault(StrConversions.CleanNumber(ExtensionsHelper.GetPrivateField<int>(__instance, "capacity")), scene.Engine);
        //_capacity.horizontal_alignment = HorizontalAlignment.Center;
        //_capacity.Margin_local = new FloatSpace(MainData.Margin_content);
        //main_grid.Transfer(_capacity, 2, 0);
        //__instance.Labels[2] = _capacity;
        InsertLabel(2, _capacity);

        // 3 min passengeres
        Label _minPass = LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.Entity.Real_min_passengers), scene.Engine);
        InsertLabel(3, _minPass);

        // 4 min%
        Label _minPerc = LabelPresets.GetDefault(StrConversions.Percent((float)__instance.Entity.Real_min_passengers / (float)__instance.Entity.Capacity), scene.Engine);
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
        float defDistance = 1000f;
        float numTrips = __instance.Entity.Speed * 24 / defDistance;
        float throughput = numTrips * (float)ExtensionsHelper.GetPrivateField<int>(__instance, "capacity");
        Label _through = LabelPresets.GetDefault(StrConversions.CleanNumber((int)throughput), scene.Engine);
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


    [HarmonyPatch("GetTooltip"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ExplorerVehicleEntity_GetTooltip_Reverse(ExplorerVehicleEntity __instance, GameScene scene) =>
        throw new NotImplementedException("ERROR. ExplorerCity_GetTooltip_Reverse");


    /* Prefix must be used because main_grid is created inside the method

    [HarmonyPatch("GetMainControl"), HarmonyPostfix]
        public static void GetMainControl(ExplorerVehicleEntity __instance, GameScene scene, Company company, CityUser city)
        {
            //Log.Write($"ORG {__instance.Labels.Length} {__instance.Labels[0]} {__instance.Labels[1]}");
            // Create a new, larger array
            Label[] newLabels = new Label[10];
            // Copy elements from the old array to the new one
            Array.Copy(__instance.Labels, newLabels, __instance.Labels.Length);
            //ExtensionsHelper.SetPrivateProperty(__instance, "Labels", newLabels); // __instance.Labels = newLabels;
            // damn... cannot use the above because only setter is private, getter is public
            // The property itself is public, so we need BindingFlags.Public.
            // However, the setter is non-public, so we'll access it separately.
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            Type type = __instance.GetType();
            PropertyInfo propertyInfo = type.GetProperty("Labels", flags);

            // Now, get the private setter method. We need to look for a non-public method.
            var setter = propertyInfo.GetSetMethod(true); // The 'true' argument is crucial.

            if (setter != null)
            {
                setter.Invoke(__instance, new object[] { newLabels });
            }
            //Log.Write($"EXT {__instance.Labels.Length} {__instance.Labels[0]} {__instance.Labels[1]}");

            // fill out new columns
            __instance.Labels[6] = LabelPresets.GetDefault("999999$", scene.Engine);
            __instance.Labels[7] = LabelPresets.GetDefault("99%", scene.Engine, Color.Lavender);
            __instance.Labels[8] = LabelPresets.GetBold("999", scene.Engine, Color.Red);
            __instance.Labels[9] = LabelPresets.GetBold("9999", scene.Engine);
        }
    */

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
            __instance.Labels[4].Text = "∞";
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


    /*
    [HarmonyPatch("Smaller"), HarmonyPrefix]
    public static bool ExplorerVehicleEntity_Smaller_Prefix(ExplorerVehicleEntity __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        Log.Write($"{__instance.Labels[0].Text} vs. {item.Labels[0].Text}");
        __result = Smaller_Modded(__instance, item, sort_id);
        return false; // skip original
    }
    */

    // Worldwide Rush, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
    // STM.UI.Explorer.ExplorerVehicleEntity
    public static bool Smaller_Modded(ExplorerVehicleEntity __instance, IExplorerItem item, int sort_id)
    {
        ExplorerVehicleEntity _item = (ExplorerVehicleEntity)item;
        if (__instance.Valid != _item.Valid)
        {
            return __instance.Valid.CompareTo(_item.Valid) > 0;
        }
        //if (sort_id == 0)
        //{
            return __instance.Labels[0].Text.CompareTo(_item.Labels[0].Text) < 0;
        //}
        /* TEMP DISABLE 
        if (sort_id - __instance.Labels.Length == 0)
        {
            return __instance.Labels[0].Text.CompareTo(_item.Labels[0].Text) > 0;
        }
        if (sort_id == 1)
        {
            int _result7 = __instance.Labels[1].Text.CompareTo(_item.Labels[1].Text);
            if (_result7 == 0)
            {
                return BackupCompare(_item);
            }
            return _result7 < 0;
        }
        if (sort_id - Labels.Length == 1)
        {
            int _result10 = __instance.Labels[1].Text.CompareTo(_item.Labels[1].Text);
            if (_result10 == 0)
            {
                return BackupCompare(_item);
            }
            return _result10 > 0;
        }
        if (sort_id == 2)
        {
            int _result9 = capacity.CompareTo(_item.capacity);
            if (_result9 == 0)
            {
                return BackupCompare(_item);
            }
            return _result9 < 0;
        }
        if (sort_id - Labels.Length == 2)
        {
            int _result8 = capacity.CompareTo(_item.capacity);
            if (_result8 == 0)
            {
                return BackupCompare(_item);
            }
            return _result8 > 0;
        }
        if (sort_id == 3)
        {
            int _result6 = Entity.Speed.CompareTo(_item.Entity.Speed);
            if (_result6 == 0)
            {
                return BackupCompare(_item);
            }
            return _result6 < 0;
        }
        if (sort_id - __instance.Labels.Length == 3)
        {
            int _result5 = Entity.Speed.CompareTo(_item.Entity.Speed);
            if (_result5 == 0)
            {
                return BackupCompare(_item);
            }
            return _result5 > 0;
        }
        if (sort_id == 4)
        {
            int _result4 = stock.CompareTo(_item.stock);
            if (_result4 == 0)
            {
                return BackupCompare(_item);
            }
            return _result4 < 0;
        }
        if (sort_id - __instance.Labels.Length == 4)
        {
            int _result3 = stock.CompareTo(_item.stock);
            if (_result3 == 0)
            {
                return BackupCompare(_item);
            }
            return _result3 > 0;
        }
        if (sort_id == 5)
        {
            int _result2 = price.CompareTo(_item.price);
            if (_result2 == 0)
            {
                return BackupCompare(_item);
            }
            return _result2 < 0;
        }
        if (sort_id - __instance.Labels.Length == 5)
        {
            int _result = price.CompareTo(_item.price);
            if (_result == 0)
            {
                return BackupCompare(_item);
            }
            return _result > 0;
        }
        return false;
        */
    }


}

/*
[HarmonyPatch(typeof(CreateNewRouteAction))]
public static class CreateNewRouteAction_Patches
{
    [HarmonyPatch("GenerateVehiclesSelection"),
        [typeof(Action<ExplorerVehicleEntity>), typeof(Func<ExplorerVehicleEntity, bool>), typeof(NewRouteSettings), typeof(IControl) parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L)
        [HarmonyPrefix]
    public static bool CreateNewRouteAction_GenerateVehiclesSelection_Prefix(CreateNewRouteAction __instance, Action<ExplorerVehicleEntity> on_select, Func<ExplorerVehicleEntity, bool> is_selected, NewRouteSettings route, IControl parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L)
    {
        Log.Write($" route={route.Name} cities={route.Cities.Count} price={price_adjust} typ={type}");
        //Log.WriteCallingStack(10);
        return true; // continue
    }

}
*/