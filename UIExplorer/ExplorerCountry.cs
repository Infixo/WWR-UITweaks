using HarmonyLib;
using STM.Data;
using STM.GameWorld;
using STM.UI;
using STM.UI.Explorer;
using STMG.Engine;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using System.Runtime.CompilerServices;
using Utilities;

namespace UITweaks.UIExplorer;


[HarmonyPatch(typeof(ExplorerCountry))]
public static class ExplorerCountry_Patches
{
    // Data extensions
    public class ExtraData
    {
        public string Name = "";
        public bool Discover = false;
        public int Delta = 0;
    }
    private static readonly ConditionalWeakTable<ExplorerCountry, ExtraData> _extras = [];
    public static ExtraData Extra(this ExplorerCountry item) => _extras.GetOrCreateValue(item);


    // Patch needed to get proper tooltips for columns.
    [HarmonyPatch(typeof(InfoUI), "OpenCountries"), HarmonyPrefix]
    public static bool InfoUI_OpenCountries_Prefix(InfoUI __instance, IControl parent, Session ___Session)
    {
        ExplorerUI<ExplorerCountry> explorerUI = 
            new ExplorerUI<ExplorerCountry>(
                __instance.CallPrivateMethod<string[]>("GetCountriesCategories", []), 
                (item) => __instance.CallPrivateMethodVoid("OnCountrySelect", [item]),
                null, 
                parent.Ui, 
                ___Session.Scene, 
                0, 
                "ve_countries", 
                (parent, id) => __instance.GetCountryTooltip(parent, id));
        explorerUI.AddItems(() => __instance.CallPrivateMethod<GrowArray<ExplorerCountry>>("GetCountries", []));
        explorerUI.AddToControlBellow(parent);
        return false;
    }

    [HarmonyPatch(typeof(InfoUI), "GetCountriesCategories"), HarmonyPrefix]
    public static bool InfoUI_GetCountriesCategories_Prefix(InfoUI __instance, ref string[] __result, Session ___Session)
    {
        __result =
        [
        Localization.GetGeneral("name"), // 0
        Localization.GetCity("cities"), // 1
        Localization.GetCity("level"), // 2
        "Avg", // 3
        "<!cicon_locate>", // 4
        Localization.GetCity("country_trust"), // 5
        ];
        if (___Session.Scene.Settings.Game_mode == GameMode.Discover)
            __result[5] = "Next <!cicon_city>";
        return false;
    }

    public static void GetCountryTooltip(this InfoUI ui, IControl parent, int id)
    {
        GameEngine engine = ui.GetPrivateField<Session>("Session").Scene.Engine;
        TooltipPreset? _tooltip = null;
        switch (id)
        {
            case 3:
                _tooltip = TooltipPreset.Get("Average city level", engine);
                break;
            case 4:
                _tooltip = TooltipPreset.Get("Not connected cities", engine);
                break;
            case 5:
                if (ui.GetPrivateField<Session>("Session").Scene.Settings.Game_mode == GameMode.Discover)
                    _tooltip = TooltipPreset.Get("Country with the highest score will spawn the next city.", engine);
                break;
        }
        _tooltip?.AddToControlBellow(parent);
    }


    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerCountry_GetMainControl_Prefix(ExplorerCountry __instance, GameScene scene)
    {
        // define more labels
        Label[] tmpLabels = new Label[6];
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

        // 0 Name
        string name = __instance.Country.Name.GetTranslation(Localization.Language);
        if (__instance.Country.Buff != null)
        {
            name += $" <!#{(LabelPresets.Color_positive * 0.85f).GetHex()}>({__instance.Country.Buff.Translated_name})";
        }
        Label _country = LabelPresets.GetDefault("<!cicon_" + __instance.Country.ISO3166_1 + ":28> " + name, scene.Engine);
        _country.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_country, 0, 0);
        __instance.Labels[0] = _country;

        // 1 Cities
        if (!__instance.Country.HasVisibleCities(scene)) // Patch 1.1.15
        {
            _country.Color = LabelPresets.Color_negative;
        }
        int cities = __instance.Country.GetVisibleCities();
        Label _cities = LabelPresets.GetDefault(StrConversions.CleanNumber(cities), scene.Engine);
        _cities.Margin_local = new FloatSpace(MainData.Margin_content);
        _cities.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_cities, 1, 0);
        __instance.Labels[1] = _cities;

        // 2 Level
        int level = __instance.Country.GetLevel();
        Label _level = LabelPresets.GetDefault("<!cicon_star> " + StrConversions.CleanNumber(level), scene.Engine);
        _level.Margin_local = new FloatSpace(MainData.Margin_content);
        _level.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_level, 2, 0);
        __instance.Labels[2] = _level;

        // 3 Average
        Label _average = LabelPresets.GetDefault(cities > 0 ? StrConversions.CleanNumber((float)level / (float)cities) : "-", scene.Engine);
        _average.Margin_local = new FloatSpace(MainData.Margin_content);
        _average.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_average, 3, 0);
        __instance.Labels[3] = _average;

        // 4 Not connected
        int connected = __instance.Country.GetConnectedCities();
        //Label _connected = LabelPresets.GetDefault(StrConversions.CleanNumber(connected) + "  (" + StrConversions.Percent((float)connected / (float)cities) + ")", scene.Engine);
        Label _connected = LabelPresets.GetDefault(StrConversions.CleanNumber(cities - connected), scene.Engine);
        if (connected == cities)
            _connected.Color = LabelPresets.Color_positive;
        else if (connected > 0)
            _connected.Color = LabelPresets.Color_negative;
        _connected.Margin_local = new FloatSpace(MainData.Margin_content);
        _connected.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_connected, 4, 0);
        __instance.Labels[4] = _connected;

        // 5 Trust
        Label _trust = LabelPresets.GetDefault("", scene.Engine);
        if (scene.Settings.Game_mode == GameMode.Discover)
        {
            __instance.Extra().Discover = true;
            if (__instance.Country.HasVisibleCities(scene))
            {
                __instance.Extra().Delta = __instance.Country.CallPrivateMethod<int>("GetVisibleCityDelta", []);
                _trust.Text = __instance.Extra().Delta.ToString();
            }
            else
                _trust.Text = "-";
            _trust.horizontal_alignment = HorizontalAlignment.Center;
        }
        else
        {
            if (__instance.Country.Dominated != ushort.MaxValue)
            {
                Company comp = scene.Session.Companies[__instance.Country.Dominated];
                __instance.Extra().Name = comp.Info.Name;
                _trust.Text = comp.GetName();
                if (comp.ID == scene.Session.Player)
                    _trust.Color = LabelPresets.Color_positive;
            }
            _trust.horizontal_alignment = HorizontalAlignment.Left;
        }
        _trust.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_trust, 5, 0);
        __instance.Labels[5] = _trust;

        // store into private fields
        __instance.SetPrivateField("main_grid", main_grid);
        __instance.SetPrivateField("main_button", main_button);
        __instance.SetPrivateField("alt", alt);
        __instance.SetPrivateField("name", name);
        __instance.SetPrivateField("level", level);

        return false; // skip the original
    }


    /// <summary>
    /// Counts connected cities in the country.
    /// </summary>
    /// <returns>Number of cities that have at least 1 route.</returns>
    public static int GetConnectedCities(this Country country)
    {
        int num = 0;
        foreach (City city in country.Cities)
            if (city.User.Routes.Count > 0)
                num++;
        return num;
    }


    [HarmonyPatch("Smaller"), HarmonyPostfix]
    public static void ExplorerCountry_Smaller_Postfix(ExplorerCountry __instance, IExplorerItem item, int sort_id, ref bool __result)
    {
        ExplorerCountry _item = (ExplorerCountry)item;
        if (__instance.Valid != _item.Valid) return; // this case was completed in the original
        if (sort_id % __instance.Labels.Length < 3) return;

        int result = 0;

        // 3 Average city level
        if (sort_id % __instance.Labels.Length == 3)
        {
            float averageThis = (float)__instance.Country.GetLevel() / (float)__instance.Country.GetVisibleCities();
            float averageItem = (float)_item.Country.GetLevel() / (float)_item.Country.GetVisibleCities();
            result = averageThis.CompareTo(averageItem);
        }

        // 4 Not connected cities
        if (sort_id % __instance.Labels.Length == 4)
        {
            int connectedThis = __instance.Country.GetVisibleCities() - __instance.Country.GetConnectedCities();
            int connectedItem = _item.Country.GetVisibleCities() - _item.Country.GetConnectedCities();
            result = connectedThis.CompareTo(connectedItem);
        }

        // 5 Trust
        if (sort_id % __instance.Labels.Length == 5)
        {
            if (__instance.Extra().Discover)
                result = __instance.Extra().Delta.CompareTo(_item.Extra().Delta);
            else
            {
                ushort domThis = __instance.Country.Dominated;
                ushort domItem = _item.Country.Dominated;
                if (domThis != ushort.MaxValue && domItem != ushort.MaxValue)
                    result = _item.Extra().Name.CompareTo(__instance.Extra().Name);
                else if (domThis == ushort.MaxValue && domItem == ushort.MaxValue)
                    result = 0;
                else
                    result = domThis == ushort.MaxValue ? -1 : +1;
            }
        }

        // Fail-safe
        if (result == 0)
            result = _item.GetPrivateField<string>("name").CompareTo(__instance.GetPrivateField<string>("name"));

        __result = sort_id < __instance.Labels.Length ? result > 0 : result < 0;
    }
}
