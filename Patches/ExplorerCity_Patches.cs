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
using Utilities;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerCity))]
public static class ExplorerCity_Patches
{
    [HarmonyPatch(typeof(InfoUI), "OpenCities"), HarmonyPrefix]
    public static bool InfoUI_OpenCities_Prefix(InfoUI __instance, IControl parent)
    {
        ExplorerUI<ExplorerCity> explorerUI = new ExplorerUI<ExplorerCity>(
            __instance.CallPrivateMethod<string[]>("GetCitiesCategories", []),
            (item) => __instance.CallPrivateMethodVoid("OnCitySelect", [item]),
            null,
            parent.Ui,
            __instance.GetPrivateField<Session>("Session").Scene,
            1,
            "ve_cities",
            (parent,id) => __instance.CallPrivateMethodVoid("GetCityTooltip", [parent,id]),
            GetCitiesFilterCategories());
        explorerUI.AddItems(() => __instance.CallPrivateMethod< GrowArray<ExplorerCity>>("GetCities", []));
        explorerUI.AddToControlBellow(parent);
        return false;
    }


    [HarmonyPatch(typeof(InfoUI), "GetCitiesCategories"), HarmonyPrefix]
    public static bool InfoUI_GetCitiesCategories_Prefix(ref string[] __result)
    {
        __result =
        [
            Localization.GetGeneral("name"), // 0
            Localization.GetCity("country"), // 1
            Localization.GetCity("level"),   // 2
            // MODDED
            Localization.GetGeneral("capacity"), // 3 "<!cicon_passenger>", //Localization.GetCity("indirect_capacity"),
            "<!cicon_passenger><!cicon_passenger><!cicon_passenger>", // 4 Biggest crowd
            Localization.GetCity("fulfillment"), // 5
            "Trust", //6 Localization.GetCity("company_trust"),
            "<!cicon_city>", // 7 Buildings Localization.GetInfrastructure("infrastructure"),
        ];
        return false; // skip original
    }


    public static FilterCategory[] GetCitiesFilterCategories()
    {
        // Buildings
        FilterCategoryItem[] buildings = new FilterCategoryItem[MainData.Buildings.Length + 1];
        buildings[0] = new FilterCategoryItem("- Reverse -");
        for (int i = 0; i < MainData.Buildings.Length; i++)
        {
            buildings[i+1] = new FilterCategoryItem(MainData.Buildings[i].Translated_name);
        }

        // type: price, values, percent, list, list_sort
        FilterCategory[] _result =
        [
            // categories: Hub, Port, Resort, Connected, Important, Blocked
            new FilterCategory(
                "Features", "list", // name + type
                new FilterCategoryItem("- Reverse -"),
                new FilterCategoryItem(Localization.GetCompany("hub")),
                new FilterCategoryItem("Connected"),
                new FilterCategoryItem("Important"),
                new FilterCategoryItem("Port"),
                new FilterCategoryItem("Resort"),
                new FilterCategoryItem("Overcrowded")),
            // buildings
            new FilterCategory(
                Localization.GetInfrastructure("infrastructure"), "list", buildings),
        ];
        return _result;
    }


    /* TODO
    [HarmonyPatch("Update"), HarmonyPostfix]
    public static void ExplorerCity_Update_Postfix(ExplorerCity __instance, GameScene scene, Company company)
    {
        // update modded data here if needed
    }
    */

    [HarmonyPatch("GetMainControl"), HarmonyPrefix]
    public static bool ExplorerCity_GetMainControl_Prefix(ExplorerCity __instance, GameScene scene)
    {
        // define more labels
        Label[] tmpLabels = new Label[8];
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

        // 0 name
        string text = __instance.City.City.Capital ? "<!cicon_train_b> " : (__instance.City.Important ? "<!cicon_plane_b> " : "<!cicon_ship_b> ");
        text += __instance.City.Name;
        if (__instance.City.Sea != null) text += "  P"; // port
        if (__instance.City.City.Resort) text += "  R"; // resort
        Label _name = LabelPresets.GetDefault(text, scene.Engine);
        InsertLabel(0, _name, HorizontalAlignment.Left);

        // 1 country
        Country _c = __instance.City.City.GetCountry(scene);
        string country = _c.Name.GetTranslation(Localization.Language);
        Label _country = LabelPresets.GetDefault("<!cicon_" + _c.ISO3166_1 + ":28> " + country, scene.Engine);
        InsertLabel(1, _country, HorizontalAlignment.Left);

        // 2 level
        string level = "<!cicon_star> " + StrConversions.CleanNumber(__instance.City.Level) + (__instance.City.Routes.Count > 0 ? " <!cicon_locate>" : "");
        Label _level = LabelPresets.GetDefault(level, scene.Engine);
        InsertLabel(2, _level);

        // 3 MODDED indirect capacity
        Label _indirect = LabelPresets.GetDefault($"{StrConversions.CleanNumber(__instance.City.GetTotalIndirect())} / {StrConversions.CleanNumber(__instance.City.GetMaxIndirect())}", scene.Engine);
        _indirect.Color = __instance.City.OvercrowdedColor(LabelPresets.Color_main);
        InsertLabel(3, _indirect);

        // 4 Biggest crowd
        InsertLabel(4, LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.City.GetBiggestCrowd()), scene.Engine));

        // 5 MODDED fulfillment
        float fulfillment = __instance.City.GetFullfilment();
        Color color = LabelPresets.Color_negative;
        if (__instance.City.GetFullfilment() > 0f) // || __instance.City.GetFullfilmentLastMonth() > 0f)
            if (fulfillment < (float)MainData.Defaults.City_shrink_threshold)
                color = Color.Yellow;
            else if (fulfillment < 1f) // && __instance.City.GetFullfilmentLastMonth() < 1f)
                color = LabelPresets.Color_main;
            else
                color = LabelPresets.Color_positive;
        Label _fulfillment = LabelPresets.GetDefault(StrConversions.Percent(fulfillment), scene.Engine);
        _fulfillment.Color = color;
        InsertLabel(5, _fulfillment);

        // 6 MODDED company_trust
        ushort player = scene.Session.Player;
        Label _trust = LabelPresets.GetDefault(StrConversions.Percent((float)__instance.City.Trust.GetPercent(player)), scene.Engine);
        if (__instance.City.Trust.Dominated == player) _trust.Color = LabelPresets.Color_positive;
        InsertLabel(6, _trust);

        // 7 MODDED infrastructure
        Hub? hub = __instance.City.GetHub(player);
        Label _infra = LabelPresets.GetDefault("", scene.Engine);
        if (hub != null)
        {
            string buildings = "";
            foreach (CityBuilding bldg in hub.Buildings.Where(b => b != null))
            {
                char code = '-';
                switch (bldg.Entity.Name)
                {
                    case "building_hotel": code = 'H'; break;
                    case "building_fuel_depot": code = 'F'; break;
                    case "building_support_center": code = 'S'; break;
                    case "building_marketing_agency": code = 'M'; break;
                    case "building_wholesale": code = 'W'; break;
                    case "building_headquarters": code = 'Q'; break;
                    default: break;
                }
                if (code != '-')
                {
                    if (buildings.Length > 0)
                        buildings += "  ";
                    buildings += new string(code, bldg.Level);
                }
            }
            if (buildings.Length == 0)
                buildings = "<!cicon_storage>";
            _infra = LabelPresets.GetBold(buildings, scene.Engine);
        }
        InsertLabel(7, _infra);

        // store into private fields
        __instance.SetPrivateField("main_grid", main_grid);
        __instance.SetPrivateField("main_button", main_button);
        __instance.SetPrivateField("alt", alt);
        __instance.SetPrivateField("country", country);

        return false; // skip the original
    }


    // Extension to get the "biggest crowd" - a destination with the higherst number of travellers
    public static int GetBiggestCrowd(this CityUser city)
    {
        Dictionary<CityUser, int> travellers = city.GetAllPassengers();
        return travellers.Count > 0 ? travellers.Values.Max() : 0;
    }

    [HarmonyPatch("Smaller"), HarmonyPostfix]
    public static void ExplorerCity_Smaller_Postfix(ExplorerCity __instance, IExplorerItem item, int sort_id, ref bool __result)
    {
        ExplorerCity _item = (ExplorerCity)item;
        if (__instance.Valid != _item.Valid) return; // this case was completed in the original

        // 3 indirect capacity
        if (sort_id % __instance.Labels.Length == 3)
        {
            float ratioThis = (float)__instance.City.GetTotalIndirect() / (float)__instance.City.GetMaxIndirect();
            float ratioItem = (float)_item.City.GetTotalIndirect() / (float)_item.City.GetMaxIndirect();
            if (sort_id < __instance.Labels.Length)
                __result = ratioThis > ratioItem;
            else
                __result = ratioThis < ratioItem;
            return;
        }

        // biggest crowd
        if (sort_id % __instance.Labels.Length == 4)
        {
            if (sort_id < __instance.Labels.Length)
                __result = __instance.City.GetBiggestCrowd() > _item.City.GetBiggestCrowd();
            else
                __result = __instance.City.GetBiggestCrowd() < _item.City.GetBiggestCrowd();
            return;
        }

        // 5 fulfillment
        if (sort_id % __instance.Labels.Length == 5)
        {
            float ratioThis = (float)__instance.City.GetTotalIndirect() / (float)__instance.City.GetMaxIndirect();
            float ratioItem = (float)_item.City.GetTotalIndirect() / (float)_item.City.GetMaxIndirect();
            if (sort_id < __instance.Labels.Length)
                __result = __instance.City.GetFullfilment() > _item.City.GetFullfilment();
            else
                __result = __instance.City.GetFullfilment() < _item.City.GetFullfilment();
            return;
        }

        // 6 trust
        if (sort_id % __instance.Labels.Length == 6)
        {
            ushort player = 0; // TODO - how to get player ID here? scene.Session.Player;
            if (sort_id < __instance.Labels.Length)
                __result = __instance.City.Trust.GetPercent(player) > _item.City.Trust.GetPercent(player);
            else
                __result = __instance.City.Trust.GetPercent(player) < _item.City.Trust.GetPercent(player);
            return;
        }

        // 7 buildings
        if (sort_id % __instance.Labels.Length == 7)
        {
            static int CountBuildings(CityUser city, ushort player)
            {
                Hub? hub = city.GetHub(player);
                if (hub == null) return -1;
                int sum = 0;
                foreach (CityBuilding bldg in hub.Buildings.Where(b => b != null))
                    sum += bldg.Level;
                return sum;
            }

            ushort player = 0; // TODO - how to get player ID here? scene.Session.Player;
            if (sort_id < __instance.Labels.Length)
                __result = CountBuildings(__instance.City, player) > CountBuildings(_item.City, player);
            else
                __result = CountBuildings(__instance.City, player) < CountBuildings(_item.City, player);
            return;
        }
    }
    

    [HarmonyPatch("Matches"), HarmonyPrefix]
    public static bool ExplorerCity_Matches_Prefix(ExplorerCity __instance, ref bool __result, FilterCategory[] categories, GameScene scene, Company company, CityUser city)
    {
        ushort player = scene.Session.Player;
        Hub _hub = __instance.City.GetHub(player);

        // Categories
        bool result0 = true;
        bool itemsSelected = false;
        for (int i = 1; i < categories[0].Items.Length; i++)
            itemsSelected |= categories[0].Items[i].Selected;
        if (itemsSelected)
        {
            if (categories[0].Items[1].Selected)
                result0 &= _hub != null;
            if (categories[0].Items[2].Selected)
                result0 &= __instance.City.Routes.Count > 0;
            if (categories[0].Items[3].Selected)
                result0 &= __instance.City.Important;
            if (categories[0].Items[4].Selected)
                result0 &= __instance.City.Sea != null;
            if (categories[0].Items[5].Selected)
                result0 &= __instance.City.City.Resort;
            if (categories[0].Items[6].Selected)
                result0 &= (__instance.City.GetTotalIndirect() * 100 / __instance.City.GetMaxIndirect()) > 100;
            if (categories[0].Items[0].Selected)
                result0 = !result0;
        }

        // Infrastructure
        bool result1 = true;
        itemsSelected = false;
        for (int i = 1; i < categories[1].Items.Length; i++)
            itemsSelected |= categories[1].Items[i].Selected;
        if (itemsSelected)
        {
            for (int i = 0; i < MainData.Buildings.Length; i++)
            {
                if (categories[1].Items[i+1].Selected)
                    result1 &= (_hub != null) && _hub.HasBuilding(MainData.Buildings[i]);
            }
            if (categories[1].Items[0].Selected)
                result1 = !result1;
        }

        __result = result0 & result1;
        return false;
    }


    [HarmonyPatch("FillCategories"), HarmonyPrefix]
    public static bool ExplorerCity_FillCategories_Prefix(ExplorerCity __instance, FilterCategory[] categories)
    {
        GameScene scene = (GameScene)GameEngine.Last.Main_scene;
        ushort player = scene.Session.Player;
        Hub _hub = __instance.City.GetHub(player);

        bool matches = false;
        _ = ExplorerCity_Matches_Prefix(__instance, ref matches, categories, scene, scene.Session.GetPlayer(), __instance.City);

        // Helper
        bool reverse0 = categories[0].Items[0].Selected;
        void IncreaseCount(int group, int index, bool flag)
        {
            if (matches && (flag ^ reverse0))
                categories[group].Items[index].IncreaseCount();
        }

        // Categories
        IncreaseCount(0, 1, _hub != null);
        IncreaseCount(0, 2, __instance.City.Routes.Count > 0);
        IncreaseCount(0, 3, __instance.City.Important);
        IncreaseCount(0, 4, __instance.City.Sea != null);
        IncreaseCount(0, 5, __instance.City.City.Resort);
        IncreaseCount(0, 6, (__instance.City.GetTotalIndirect() * 100 / __instance.City.GetMaxIndirect()) > 100);

        for (int i = 0; i < MainData.Buildings.Length; i++)
            IncreaseCount(1, i+1, (_hub != null) && _hub.HasBuilding(MainData.Buildings[i]));

        return false;
    }

    /*
    [HarmonyPatch("SetSizes"), HarmonyPostfix]
    public static void ExplorerCity_SetSizes_Postfix(ExplorerCity __instance, int[] sizes)
    {
        //string line = "";
        //foreach (int size in sizes) line += " " + size;
        //Log.Write(line);
        Grid _main_grid = ExtensionsHelper.GetPrivateField<Grid>(__instance, "main_grid");
        _main_grid.SetColumn(0, SizeType.Pixels, sizes[0]*80/100);
        _main_grid.SetColumn(1, SizeType.Pixels, sizes[1]*60/100);
    }
    */
}
