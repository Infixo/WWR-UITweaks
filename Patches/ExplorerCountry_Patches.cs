using HarmonyLib;
using Utilities;
using STM.Data;
using STM.GameWorld;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerCountry))]
public static class ExplorerCountry_Patches
{
    [HarmonyPatch(typeof(InfoUI), "GetCountriesCategories"), HarmonyPrefix]
    public static bool InfoUI_GetCountriesCategories_Prefix(ref string[] __result)
    {
        __result =
        [
        Localization.GetGeneral("name"),
        Localization.GetCity("cities"),
        Localization.GetCity("level"),
        "Avg",
        "<!cicon_locate>"
        ];
        return false;
    }


    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerCountry_GetMainControl_Prefix(ExplorerCountry __instance, GameScene scene)
    {
        // define more labels
        Label[] tmpLabels = new Label[5];
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
        if (!__instance.Country.HasVisibleCities())
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
        Label _average = LabelPresets.GetDefault(StrConversions.CleanNumber((float)level / (float)cities), scene.Engine);
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

        // 3 Average
        if (sort_id % __instance.Labels.Length == 3)
        {
            float averageThis = (float)__instance.Country.GetLevel() / (float)__instance.Country.GetVisibleCities();
            float averageItem = (float)_item.Country.GetLevel() / (float)_item.Country.GetVisibleCities();
            if (sort_id < __instance.Labels.Length)
                __result = averageThis > averageItem;
            else
                __result = averageThis < averageItem;
            return;
        }

        // 4 Connected
        if (sort_id % __instance.Labels.Length == 4)
        {
            /* sorts by percent of connected cities
            float connectedThis = (float)__instance.Country.GetConnectedCities() / (float)__instance.Country.GetVisibleCities();
            float connectedItem = (float)_item.Country.GetConnectedCities() / (float)_item.Country.GetVisibleCities();
            */
            // sorts by number of not-connected cities
            int connectedThis = __instance.Country.GetVisibleCities() - __instance.Country.GetConnectedCities();
            int connectedItem = _item.Country.GetVisibleCities() - _item.Country.GetConnectedCities();
            if (sort_id < __instance.Labels.Length)
                __result = connectedThis > connectedItem;
            else
                __result = connectedThis < connectedItem;
            return;
        }
    }
}
