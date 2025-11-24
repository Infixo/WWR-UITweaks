using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.UI;
using STM.UI.Explorer;
using STMG.Engine;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using Utilities;

namespace UITweaks.UIExplorer;


[HarmonyPatch(typeof(ExplorerCompany))]
public static class ExplorerCompany_Patches
{
    [HarmonyPatch(typeof(InfoUI), "GetCompaniesCategories"), HarmonyPrefix]
    public static bool InfoUI_GetCompaniesCategories_Prefix(ref string[] __result)
    {
        __result =
        [
            Localization.GetGeneral("name"), // 0
            "Value", //Localization.GetCompany("company_value"), // 1
            "Profit", //Localization.GetCompany("operating_profit"), // 2
            "ROA", // 3
            Localization.GetCity("level"), // 4 Level
            "Shares", //Localization.GetCompany("company_shares"), // 5
            Localization.GetCompany("hubs"), // 6
            Localization.GetInfrastructure("vehicles"), // 7
        ];
        return false;
    }


    private const int NameWidth = 520;

    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerCompany_GetMainControl_Prefix(ExplorerCompany __instance, GameScene scene)
    {
        Label[] Labels = new Label[8];
        __instance.SetPublicProperty("Labels", Labels);

        // Main button
        Button main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, 32, 1f), scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: false, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        __instance.SetPrivateField("main_button", main_button);
        main_button.Opacity = 0f;
        main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        main_button.OnMouseStillTime += (Action)delegate
        {
            __instance.CallPrivateMethodVoid("GetTooltip", [scene]);
        };

        Image alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        __instance.SetPrivateField("alt", alt);
        alt.Opacity = 0f;
        _collection.Transfer(alt);

        // Grid
        Grid main_grid = new Grid(ContentRectangle.Stretched, Labels.Length, 1, SizeType.Weight);
        __instance.SetPrivateField("main_grid", main_grid);
        main_grid.OnFirstUpdate += () => main_grid.update_children = false;
        main_grid.OnUpdate += () => main_grid[2].OnUpdate.Invoke(); // scroll for name
        _collection.Transfer(main_grid);

        // Logo
        Image _back = new Image(MainData.Logos_back[__instance.Company.Info.Logo]);
        _back.vertical_alignment = VerticalAlignment.Center;
        _back.Color = __instance.Company.Color_secondary;
        _back.Zoom_local = 0.2f;
        main_grid.Transfer(_back, 0, 0);
        Image _front = new Image(MainData.Logos_front[__instance.Company.Info.Logo]);
        _front.vertical_alignment = VerticalAlignment.Center;
        _front.Color = __instance.Company.Color_main;
        _front.Zoom_local = 0.2f;
        main_grid.Transfer(_front, 0, 0);

        int col = 0;

        // 0 Name
        string _nameTxt = __instance.Company.Info.Name;
        if (__instance.Company.Info is CompanyGenerated _info)
            _nameTxt += $" <!#{(LabelPresets.Color_main * 0.75f).GetHex()}>({_info.GetCountry(scene).Name.GetTranslation(Localization.Language)})";
        Label _name = LabelPresets.GetDefault(_nameTxt, scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content * 3);
        IControl _radio = LabelPresets.GetRadio(_name, NameWidth); // scroll
        _radio.Mouse_visible = false;
        main_grid.Transfer(_radio, col, 0);
        Labels[col++] = _name;
        if (__instance.Company.Bankrupt)
            _name.Color = LabelPresets.Color_negative;
        else if (__instance.Company.ID == scene.Session.Player)
            _name.Color = LabelPresets.Color_positive;

        // 1 Value
        Label _value = LabelPresets.GetDefault("999", scene.Engine);
        _value.Margin_local = new FloatSpace(MainData.Margin_content);
        _value.horizontal_alignment = HorizontalAlignment.Right;
        main_grid.Transfer(_value, col, 0);
        Labels[col++] = _value;

        // 2 Profit
        Label _balance = LabelPresets.GetDefault("999", scene.Engine);
        _balance.Margin_local = new FloatSpace(MainData.Margin_content);
        _balance.horizontal_alignment = HorizontalAlignment.Right;
        main_grid.Transfer(_balance, col, 0);
        Labels[col++] = _balance;

        // 3 ROA
        Label _roa = LabelPresets.GetDefault("9.9%", scene.Engine);
        _roa.Margin_local = new FloatSpace(MainData.Margin_content);
        _roa.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_roa, col, 0);
        Labels[col++] = _roa;

        // 4 Level
        Label _level = LabelPresets.GetDefault("<!cicon_star> " + STM.GameWorld.Tasks.CompanyTask.GetLevel(__instance.Company, true).ToString(), scene.Engine);
        _level.Margin_local = new FloatSpace(MainData.Margin_content);
        _level.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_level, col, 0);
        Labels[col++] = _level;

        // 5 Shares
        float _owned = __instance.Company.Shares.GetOwnedBy(scene.Session.GetPlayer());
        Label _shares = LabelPresets.GetDefault(_owned > 0 ? StrConversions.Percent(_owned) : "", scene.Engine);
        if (_owned > 0.5f)
            _shares.Color = LabelPresets.Color_positive;
        _shares.Margin_local = new FloatSpace(MainData.Margin_content);
        _shares.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_shares, col, 0);
        Labels[col++] = _shares;

        // 6 Hubs
        Label _hubs = LabelPresets.GetDefault(__instance.Company.Hubs.ToString(), scene.Engine);
        _hubs.Margin_local = new FloatSpace(MainData.Margin_content);
        _hubs.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_hubs, col, 0);
        Labels[col++] = _hubs;

        // 7 Vehicles
        Label _vehicles = LabelPresets.GetDefault(__instance.Company.GetVehiclesWithIcons(scene), scene.Engine);
        _vehicles.Margin_local = new FloatSpace(MainData.Margin_content);
        _vehicles.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_vehicles, col, 0);
        Labels[col++] = _vehicles;

        return false;
    }


    internal static string GetVehiclesWithIcons(this Company company, GameScene scene)
    {
        CompanyGenerated.CompanyType _type = (CompanyGenerated.CompanyType)scene.Settings.Vehicles_flag;
        if (company.Info is CompanyGenerated _info)
            _type = _info.Company_type;
        string _res = company.Vehicles.ToString() + "  |";
        if ((_type & CompanyGenerated.CompanyType.Road_vehicles) != 0) _res += $" <!cicon_road_vehicle>{company.Road_vehicles}";
        if ((_type & CompanyGenerated.CompanyType.Trains) != 0) _res += $" <!cicon_train>{company.Trains}";
        if ((_type & CompanyGenerated.CompanyType.Planes) != 0) _res += $" <!cicon_plane>{company.Planes}";
        if ((_type & CompanyGenerated.CompanyType.Ships) != 0) _res += $" <!cicon_ship>{company.Ships}";
        return _res;
    }


    [HarmonyPatch("Update"), HarmonyPostfix]
    public static void ExplorerCompany_Update_Postfix(ExplorerCompany __instance, GameScene scene, Company company, long ___value, long ___profit)
    {
        float _roa = ___value > 100 ? 100f * (float)___profit / (float)___value : 0;
        __instance.Labels[3].Text = ___value > 100 ? $"{_roa:F1}%" : "-";
        __instance.Labels[3].Color = _roa < 0 || ___value <= 100 ? LabelPresets.Color_negative : LabelPresets.Color_positive;
        __instance.Labels[6].Text = __instance.Company.Hubs.ToString();
        __instance.Labels[7].Text = __instance.Company.GetVehiclesWithIcons(scene);
    }


    [HarmonyPatch("Smaller"), HarmonyPostfix]
    public static void ExplorerCompany_Smaller_Postfix(ExplorerCompany __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        ExplorerCompany _item = (ExplorerCompany)item;
        if (__instance.Valid != _item.Valid) return;
        if (__instance.Company.Bankrupt || _item.Company.Bankrupt) return;
        if (sort_id % __instance.Labels.Length < 3) return; // this case was completed in the original

        GameScene scene = (GameScene)GameEngine.Last.Main_scene;
        int result = 0;

        // 3 ROA
        if (sort_id % __instance.Labels.Length == 3)
        {
            long valueThis = __instance.GetPrivateField<long>("value");
            long valueItem = _item.GetPrivateField<long>("value");
            if (valueThis > 100 && valueItem > 100)
            {
                long profitThis = __instance.GetPrivateField<long>("profit");
                long profitItem = _item.GetPrivateField<long>("profit");
                result = (1000L * profitThis / valueThis).CompareTo(1000L * profitItem / valueItem);
            }
            else
                result = valueThis.CompareTo(valueItem);
        }

        // 4 Level
        if (sort_id % __instance.Labels.Length == 4)
        {
            Company player = scene.Session.GetPlayer();
            result = STM.GameWorld.Tasks.CompanyTask.GetLevel(__instance.Company, true).CompareTo(STM.GameWorld.Tasks.CompanyTask.GetLevel(_item.Company, true));
            if (result == 0)
                result = __instance.GetPrivateField<long>("value").CompareTo(_item.GetPrivateField<long>("value"));
        }

        // 5 Shares
        if (sort_id % __instance.Labels.Length == 5)
        {
            Company player = scene.Session.GetPlayer();
            result = __instance.Company.Shares.GetOwnedBy(player).CompareTo(_item.Company.Shares.GetOwnedBy(player));
            if (result == 0)
                result = __instance.GetPrivateField<long>("value").CompareTo(_item.GetPrivateField<long>("value"));
        }

        // 6 Hubs
        if (sort_id % __instance.Labels.Length == 6)
        {
            result = __instance.Company.Hubs.CompareTo(_item.Company.Hubs);
            if (result == 0)
                result = __instance.Company.Vehicles.CompareTo(_item.Company.Vehicles);
        }

        // 7 Vehicles
        if (sort_id % __instance.Labels.Length == 7)
        {
            result = __instance.Company.Vehicles.CompareTo(_item.Company.Vehicles);
            if (result == 0)
                result = __instance.Company.Hubs.CompareTo(_item.Company.Hubs);
        }

        // Fail-safe
        if (result == 0)
            result = __instance.Company.Info.Name.CompareTo(_item.Company.Info.Name);

        __result = sort_id < __instance.Labels.Length ? result > 0 : result < 0;
    }


    [HarmonyPatch("GetSizes"), HarmonyPostfix]
    public static void ExplorerCompany_GetSizes_Postfix(int[] sizes)
    {
        sizes[0] = NameWidth; // name
    }
}
