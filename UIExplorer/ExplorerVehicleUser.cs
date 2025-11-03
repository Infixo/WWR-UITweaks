using Microsoft.Xna.Framework;
using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STM.UI.Stats;
using STMG.UI.Control;
using STVisual.Utility;
using Utilities;

namespace UITweaks.UIExplorer;


[HarmonyPatch(typeof(ExplorerVehicleUser))]
public static class ExplorerVehicleUser_Patches
{
    [HarmonyPatch(typeof(InfoUI), "GetCategories"), HarmonyPrefix]
    public static bool InfoUI_GetCategories_Prefix(ref string[] __result)
    {
        __result =
        [
            Localization.GetGeneral("name"),
            Localization.GetCompany("company"),
            Localization.GetVehicle("route"),
            Localization.GetGeneral("capacity"),
            Localization.GetGeneral("efficiency"),
            Localization.GetGeneral("balance"),
            // added
            "<!cicon_passenger> <!cicon_passenger>", // quarter efficiency
            "<!cicon_fast> <!cicon_fast>", // quarter throughput
        ];
        return false;
    }


    [HarmonyPatch(typeof(InfoUI), "GetTooltip"), HarmonyPostfix]
    public static void RouteUI_GetTooltip_Postfix(IControl parent, int category, Session ___Session)
    {
        TooltipPreset? _tooltip = null;
        switch (category)
        {
            case 6:
                _tooltip = TooltipPreset.Get("Quarter Efficiency", ___Session.Scene.Engine);
                _tooltip.AddDescription("Average efficiency in the last quarter.");
                break;
            case 7:
                _tooltip = TooltipPreset.Get("Quarter Throughput", ___Session.Scene.Engine);
                _tooltip.AddDescription("Average throughput in the last quarter");
                break;
        }
        _tooltip?.AddToControlBellow(parent);
    }


    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerVehicleUser_GetMainControl_Prefix(ExplorerVehicleUser __instance, GameScene ___scene,
        ref Button ___main_button, ref Image ___alt, ref Image ___selection, ref Image ___selection_icon)
    {
        __instance.SetPublicProperty("Labels", new Label[8]);
        int _height = 32;
        ___main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, _height, 1f), ___scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: true, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        ___main_button.Opacity = 0f;
        ___main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        ___main_button.OnMouseStillTime += (Action)delegate
        {
            __instance.CallPrivateMethodVoid("GetTooltip", [___scene]);
        };
        ___alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        ___alt.Opacity = 0f;
        _collection.Transfer(___alt);
        ___selection = new Image(ContentRectangle.Stretched, MainData.Panel_gradient_left);
        ___selection.Margin_local = new FloatSpace(0f, 0f);
        ___selection.Opacity = (__instance.Selected ? 1f : 0f);
        ___selection.Color = LabelPresets.Color_main;
        _collection.Transfer(___selection);
        Grid main_grid = new Grid(ContentRectangle.Stretched, __instance.Labels.Length + 2, 1, SizeType.Weight);
        __instance.SetPrivateField("main_grid", main_grid);
        main_grid.OnFirstUpdate += (Action)delegate
        {
            main_grid.update_children = false;
        };
        main_grid.OnUpdate += (Action)delegate
        {
            main_grid[2].OnUpdate.Invoke();
        };
        _collection.Transfer(main_grid);
        main_grid.SetColumn(0, SizeType.Pixels, MainData.Size_button);
        main_grid.SetColumn(1, SizeType.Pixels, MainData.Margin_content_items);
        ButtonItem _select = ButtonPresets.IconBlack(new ContentRectangle(0f, 0f, MainData.Size_button, _height, 1f), __instance.Selected ? MainData.Icon_toggle_on : MainData.Icon_toggle_off, ___scene.Engine);
        _collection.Transfer(_select.Control);
        ___selection_icon = _select.Icon;
        _select.Control.OnButtonPress += (Action)delegate
        {
            __instance.SetSelected(!__instance.Selected);
            if (__instance.Selected)
            {
                __instance.GetPrivateField<ExplorerUI<ExplorerVehicleUser>>("explorer").AddSelected(__instance);
            }
            else
            {
                __instance.GetPrivateField<ExplorerUI<ExplorerVehicleUser>>("explorer").RemoveSelected(__instance);
            }
        };
        // 0 Name
        Label _name = LabelPresets.GetDefault(__instance.User.GetName(), ___scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_name, 2, 0);
        __instance.Labels[0] = _name;
        // 1 Company
        Label _company = LabelPresets.GetDefault(__instance.User.Entity_base.Company.Entity.Translated_name, ___scene.Engine);
        _company.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_company, 3, 0);
        __instance.Labels[1] = _company;
        // 2 Route
        Label _route = LabelPresets.GetDefault(typeof(ExplorerVehicleUser).CallPrivateStaticMethod<string>("GetCurrentRoute", [__instance.User, ___scene]), ___scene.Engine);
        _route.Margin_local = new FloatSpace(MainData.Margin_content);
        IControl _radio = LabelPresets.GetRadio(_route, 400);
        _radio.Mouse_visible = false;
        main_grid.Transfer(_radio, 4, 0);
        __instance.Labels[2] = _route;
        // 3 Capacity
        Label _capacity = LabelPresets.GetDefault("", ___scene.Engine);
        _capacity.horizontal_alignment = HorizontalAlignment.Center;
        _capacity.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_capacity, 5, 0);
        __instance.Labels[3] = _capacity;
        // 4 Efficiency
        Label _efficiency = LabelPresets.GetDefault("", ___scene.Engine);
        _efficiency.horizontal_alignment = HorizontalAlignment.Center;
        _efficiency.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_efficiency, 6, 0);
        __instance.Labels[4] = _efficiency;
        // 5 Balance
        Label _balance = LabelPresets.GetDefault(StrConversions.GetBalanceWithPlus(__instance.User.Balance.GetCurrentMonth(), ___scene.currency), ___scene.Engine);
        _balance.horizontal_alignment = HorizontalAlignment.Right;
        _balance.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_balance, 7, 0);
        __instance.Labels[5] = _balance;
        // 6 Quarter efficiency
        Label _qEff = LabelPresets.GetDefault(StrConversions.Percent((float)__instance.User.Efficiency.GetQuarterAverage()/100f), ___scene.Engine);
        _qEff.horizontal_alignment = HorizontalAlignment.Center;
        _qEff.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_qEff, 8, 0);
        __instance.Labels[6] = _qEff;
        // 7 Quarter throughput
        Label _throughput = LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.User.Throughput.GetQuarterAverage()), ___scene.Engine);
        _throughput.horizontal_alignment = HorizontalAlignment.Center;
        _throughput.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_throughput, 9, 0);
        __instance.Labels[7] = _throughput;
        return false;
    }


    [HarmonyPatch("Update"), HarmonyPostfix]
    public static void ExplorerVehicleUser_Update_Postfix(ExplorerVehicleUser __instance, GameScene scene, Company company)
    {
        __instance.Labels[6].Text = StrConversions.Percent((float)__instance.User.Efficiency.GetQuarterAverage()/100f);
        __instance.Labels[7].Text = StrConversions.CleanNumber(__instance.User.Throughput.GetQuarterAverage());
    }


    [HarmonyPatch("Smaller"), HarmonyPostfix]
    public static void ExplorerVehicleUser_Smaller_Postfix(ExplorerVehicleUser __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        ExplorerVehicleUser _item = (ExplorerVehicleUser)item;
        if (__instance.Valid != _item.Valid) return;

        // 6 Efficieny
        if (sort_id % __instance.Labels.Length == 6)
        {
            int result = __instance.User.Efficiency.GetQuarterAverage().CompareTo(_item.User.Efficiency.GetQuarterAverage());
            if (result == 0)
                result = __instance.User.ID.CompareTo(_item.User.ID);
            __result = sort_id < __instance.Labels.Length ? result > 0 : result < 0;
        }
    
        // 7 Throughput
        if (sort_id % __instance.Labels.Length == 7)
        {
            int result = __instance.User.Throughput.GetQuarterAverage().CompareTo(_item.User.Throughput.GetQuarterAverage());
            if (result == 0)
                result = __instance.User.ID.CompareTo(_item.User.ID);
            __result = sort_id < __instance.Labels.Length ? result > 0 : result < 0;
        }
    }


    // Modded: info about hub and position of the vehicle on the route.
    [HarmonyPatch("AddVehicleInfo"), HarmonyPrefix]
    public static bool ExplorerVehicleUser_AddVehicleInfo_Prefix(TooltipPreset tooltip, VehicleBaseUser vehicle, GameScene scene)
    {
        tooltip.AddSeparator();
        tooltip.AddBoldLabel(Localization.GetGeneral("balance"), null, center: true);
        Company _company = scene.Session.Companies[vehicle.Company];
        IControl _graph = ChartLine.GetSingle(new GraphSettings((int t) => typeof(ExplorerVehicleUser).CallPrivateStaticMethod<long>("GetValue", [t, vehicle]), 12, -1L, -1L, 2L)
        {
            update_tooltip = delegate (TooltipPreset t, GraphSettings s, int id)
            {
                ExplorerVehicleUser.UpdateGraphTooltip(t, s, id, vehicle, scene);
            }
        }, fill: true, _company.GetGridColor());
        _graph.vertical_alignment = VerticalAlignment.Top;
        _graph.Margin_local = new FloatSpace(MainData.Margin_content);
        _graph.Size_local = new Vector2(200f, 100f);
        tooltip.AddContent(_graph);
        // Route
        tooltip.AddSeparator();
        tooltip.AddBoldLabel(vehicle.GetLine(scene)?.GetName() ?? Localization.GetVehicle("route"), null, center: true);
        for (int i = 0; i < vehicle.Route.Instructions.Cities.Length; i++)
        {
            CityUser _city = vehicle.Route.Instructions.Cities[i];
            string _hub = vehicle.Hub.City == _city.City.City_id ? "  <!cicon_storage>" : "";
            string _prefix = _city == vehicle.Route.Current ? "<!cicon_ship_b> " : (_city == vehicle.Route.Destination && vehicle.Route.Moving ? vehicle.Route.GetProgressIcon() : "");
            tooltip.AddStatsLine(_prefix + _city.GetNameWithIcon(scene) + _hub, (i + 1).ToString());
        }
        return false;
    }

    // Helper for the progress icon
    internal static string GetProgressIcon(this RouteInstance route)
    {
        if (route.Progress > 0.9m) return "<!cicon_fastest>";
        if (route.Progress > 0.75m) return "<!cicon_faster>";
        if (route.Progress > 0.5m) return "<!cicon_fast>";
        return "<!cicon_right>";
    }
}
