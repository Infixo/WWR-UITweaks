using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.AI;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using System.Runtime.CompilerServices;
using Utilities;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerHubs))]
public static class ExplorerHubs_Patches
{
    // Data extensions
    public class ExtraData
    {
        public long Budget; // budget in millions, -1 is Auto, -2 is n/a
    }
    private static readonly ConditionalWeakTable<ExplorerHubs, ExtraData> _extras = [];
    public static ExtraData Extra(this ExplorerHubs hub) => _extras.GetOrCreateValue(hub);


    [HarmonyPatch(typeof(InfoUI), "GetHubsCategories"), HarmonyPrefix]
    public static bool ExplorerHubs_GetHubsCategories_Prefix(ref string[] __result)
    {
        __result =
        [
        Localization.GetGeneral("class_city"),
        Localization.GetCity("level"),
        "<!cicon_passenger>", // Localization.GetCity("indirect_capacity"),
        Localization.GetInfrastructure("vehicles"),
        Localization.GetCompany("manager_goal"),
        Localization.GetGeneral("balance"),
        // added
        Localization.GetCompany("budget"),
        "<!cicon_road_vehicle><!cicon_train><!cicon_plane><!cicon_ship>",
        ];
        return false;
    }


    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool GetMainControl(ExplorerHubs __instance, GameScene scene, ref Button ___main_button, ref Image ___alt)
    {
        // define more labels
        Label[] tmpLabels = new Label[8];
        ExtensionsHelper.SetPublicProperty(__instance, "Labels", tmpLabels);

        int _height = 32;
        ___main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, _height, 1f), scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: false, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        ___main_button.Opacity = 0f;
        ___main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        ___main_button.OnMouseStillTime += (Action)delegate
        {
            __instance.CallPrivateMethodVoid("GetTooltip", [scene]);
        };

        ___alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        ___alt.Opacity = 0f;
        _collection.Transfer(___alt);

        Grid main_grid = new Grid(ContentRectangle.Stretched, __instance.Labels.Length, 1, SizeType.Weight);
        __instance.SetPrivateField("main_grid", main_grid);
        main_grid.OnFirstUpdate += (Action)delegate
        {
            main_grid.update_children = false;
        };
        _collection.Transfer(main_grid);

        // 1 Name
        string name = __instance.City.GetNameWithIcon(scene);
        name += $" <!#{(LabelPresets.Color_main * 0.75f).GetHex()}>({__instance.City.City.GetCountry(scene).Name.GetTranslation(Localization.Language)})";
        Label _name = LabelPresets.GetDefault(name, scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_name, 0, 0);
        __instance.Labels[0] = _name;

        // 1 Level
        Label _level = LabelPresets.GetDefault("<!cicon_star> " + StrConversions.CleanNumber(__instance.Hub.Level), scene.Engine);
        _level.Margin_local = new FloatSpace(MainData.Margin_content);
        _level.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_level, 1, 0);
        __instance.Labels[1] = _level;

        // 2 Indirect
        Label _indirect = LabelPresets.GetDefault(StrConversions.OutOf(__instance.City.GetTotalIndirect(), __instance.City.GetMaxIndirect()), scene.Engine);
        _indirect.Margin_local = new FloatSpace(MainData.Margin_content);
        _indirect.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_indirect, 2, 0);
        __instance.Labels[2] = _indirect;

        // 3 Vehicles
        HubManager manager = __instance.Hub.Manager;
        Label _vehicles = LabelPresets.GetDefault(StrConversions.OutOf(__instance.Hub.Vehicles.Count, __instance.Hub.Level * MainData.Defaults.Hub_max_vehicles), scene.Engine);
        _vehicles.Margin_local = new FloatSpace(MainData.Margin_content);
        _vehicles.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_vehicles, 3, 0);
        __instance.Labels[3] = _vehicles;

        if (manager != null)
        {
            int numGenPlans = manager.GetPrivateField<GrowArray<GeneratedPlan>>("generated").Count;
            if (numGenPlans > 0)
            {
                _vehicles.Text += $"  [{StrConversions.CleanNumberWithPlus(numGenPlans)}]";
                _vehicles.Color = LabelPresets.Color_positive;
            }
        }

        // 4 Goal
        Label _goal = LabelPresets.GetBold(__instance.GetGoalEx(), scene.Engine);
        _goal.Margin_local = new FloatSpace(MainData.Margin_content);
        _goal.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_goal, 4, 0);
        __instance.Labels[4] = _goal;

        // 5 Balance
        Label _balance = LabelPresets.GetDefault("", scene.Engine);
        _balance.Margin_local = new FloatSpace(MainData.Margin_content);
        _balance.horizontal_alignment = HorizontalAlignment.Right;
        main_grid.Transfer(_balance, 5, 0);
        __instance.Labels[5] = _balance;

        // 6 Budget
        string budget = "-";
        __instance.Extra().Budget = -2;
        if ( manager != null)
        {
            __instance.Extra().Budget = manager.budget == 0 ? -1 : (((long)scene.currency.InCurrency(manager.budget))+500000L)/1000000L;
            budget = __instance.Extra().Budget >= 0 ? StrConversions.CleanNumber(__instance.Extra().Budget) + "M" : Localization.GetGeneral("auto");
        }
        Label _budget = LabelPresets.GetDefault(budget, scene.Engine);
        _budget.Margin_local = new FloatSpace(MainData.Margin_content);
        _budget.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_budget, 6, 0);
        __instance.Labels[6] = _budget;

        // 7 Brands
        string brands = "-";
        if (manager != null)
        {
            brands = ""; 
            if (manager.buy_road_vehicles) brands += "<!cicon_road_vehicle>";
            if (manager.buy_trains) brands += "<!cicon_train>";
            if (manager.buy_planes) brands += "<!cicon_plane>";
            if (manager.buy_ships) brands += "<!cicon_ship>";
            // Vehicle companies
            if (manager.brands != null)
            {
                brands += "  ";
                brands += string.Join(" ",
                    MainData.Vehicle_companies
                    .Where(item => __instance.Hub.Manager.brands.Contains((ushort)item.ID))
                    .Select(item => /*WorldwideRushExtensions.GetVehicleTypeIcon(item.Vehicles[0].Type_name) +*/ item.Translated_name[..2]));
            }
        }

        Label _brands = LabelPresets.GetDefault(brands, scene.Engine);
        _brands.Margin_local = new FloatSpace(MainData.Margin_content);
        //_brands.horizontal_alignment = HorizontalAlignment.Left;
        //IControl _radio = LabelPresets.GetRadio(_brands, 100, true); // scroll
        //_radio.Mouse_visible = false;
        main_grid.Transfer(_brands, 7, 0);
        __instance.Labels[7] = _brands;

        return false;
    }

    /*
    [HarmonyPatch("GetSizes"), HarmonyPostfix]
    public static void ExplorerHubs_GetSizes_Postfix(int[] sizes)
    {
        sizes[7] = 200;
    }
    */

    public static string GetGoalEx(this ExplorerHubs hub)
    {
        HubManager manager = hub.Hub.Manager;
        if (manager == null) return "-";
        // goal
        string goal = manager.Only_manage ? "M " : "E ";
        if (manager.buy_roads) goal += "<!cicon_road_vehicle>";
        if (manager.buy_rails) goal += "<!cicon_train>";
        return goal;
    }


    public static decimal InCurrency(this CurrencyEntity entity, decimal number)
    {
        return Math.Ceiling((decimal)number * entity.Factor / 100m);
    }


    [HarmonyPatch("Smaller"), HarmonyPostfix]
    public static void ExplorerHubs_Smaller_Postfix(ExplorerHubs __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        ExplorerHubs _item = (ExplorerHubs)item;
        if (__instance.Valid != _item.Valid) return;

        // 6 Budget
        if (sort_id % __instance.Labels.Length == 6)
        {
            long thisBudget = __instance.Extra().Budget;
            long itemBudget = _item.Extra().Budget;
            int result = thisBudget.CompareTo(itemBudget);
            if (result == 0)
                result = __instance.Hub.GetQuarterAverage().CompareTo(_item.Hub.GetQuarterAverage());
            __result = sort_id < __instance.Labels.Length ? result > 0 : result < 0;
        }

        // 7 Brands
        if (sort_id % __instance.Labels.Length == 7)
        {
            int result = __instance.Labels[7].Text.CompareTo(_item.Labels[7].Text);
            if (result == 0)
                result = __instance.City.Name.CompareTo(_item.City.Name);
            __result = sort_id < __instance.Labels.Length ? result > 0 : result < 0;
        }
    }


    [HarmonyPatch("Update"), HarmonyPrefix]
    public static bool ExplorerHubs_Update_Prefix(ExplorerHubs __instance, GameScene scene, Company company, ref long ___balance, PathArrow ___path)
    {
        ___path?.Update(scene.UI.Frame_time, scene);
        int _current = __instance.City.GetTotalIndirect();
        int _total = __instance.City.GetMaxIndirect();
        __instance.Labels[2].Text = StrConversions.OutOf(_current, _total);
        __instance.Labels[2].Color = __instance.City.OvercrowdedColor(LabelPresets.Color_main);
        ___balance = __instance.Hub.GetQuarterAverage();
        __instance.Labels[5].Text = StrConversions.GetBalanceWithPlus(___balance, scene.currency);
        __instance.Labels[5].Color = ((___balance >= 0) ? LabelPresets.Color_positive : LabelPresets.Color_negative);
        return false;
    }
}
