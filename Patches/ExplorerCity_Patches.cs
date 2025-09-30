using System.Runtime.CompilerServices;
using HarmonyLib;
using Utilities;
using Microsoft.Xna.Framework;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerCity))]
public static class ExplorerCity_Patches
{

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
            //ExplorerCity_GetTooltip_Reverse(__instance, scene);
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
        Label _name = LabelPresets.GetDefault(__instance.City.Name, scene.Engine);
        //_name.Margin_local = new FloatSpace(MainData.Margin_content);
        //main_grid.Transfer(_name, 0, 0);
        //__instance.Labels[0] = _name;
        InsertLabel(0, _name, HorizontalAlignment.Left);

        // 1 country
        Country _c = __instance.City.City.GetCountry(scene);
        string country = _c.Name.GetTranslation(Localization.Language);
        Label _country = LabelPresets.GetDefault("<!cicon_" + _c.ISO3166_1 + ":28> " + country, scene.Engine);
        InsertLabel(1, _country, HorizontalAlignment.Left);

        // 2 level
        ushort player = scene.Session.Player;
        Hub? hub = __instance.City.GetHub(player);
        string level = "<!cicon_star> " + StrConversions.CleanNumber(__instance.City.Level);
        Label _level = LabelPresets.GetDefault(level, scene.Engine);
        InsertLabel(2, _level);

        // 3 MODDED indirect capacity
        Label _indirect = LabelPresets.GetDefault($"{StrConversions.CleanNumber(__instance.City.GetTotalIndirect())} / {StrConversions.CleanNumber(__instance.City.GetMaxIndirect())}", scene.Engine);
        _indirect.Color = CityWorldGraphics_Patches.OvercrowdedColor(__instance.City, LabelPresets.Color_main);
        InsertLabel(3, _indirect);

        // 4 Biggest crowd
        InsertLabel(4, LabelPresets.GetDefault(StrConversions.CleanNumber(__instance.City.GetBiggestCrowd()), scene.Engine));

        // 5 MODDED fulfillment
        string fulfillment = StrConversions.Percent(0f);
        Color color = LabelPresets.Color_negative;
        if (__instance.City.GetFullfilment() > 0f) // || __instance.City.GetFullfilmentLastMonth() > 0f)
            if (__instance.City.GetFullfilment() < 1f) // && __instance.City.GetFullfilmentLastMonth() < 1f)
            {
                fulfillment = StrConversions.Percent(__instance.City.GetFullfilment()); // + "  (" + StrConversions.Percent(__instance.City.GetFullfilmentLastMonth()) + ")";
                color = LabelPresets.Color_main;
            }
            else
            {
                fulfillment = StrConversions.Percent(1f);
                color = LabelPresets.Color_positive;
            }
        Label _fulfillment = LabelPresets.GetDefault(fulfillment, scene.Engine);
        _fulfillment.Color = color;
        InsertLabel(5, _fulfillment);

        // 6 MODDED company_trust
        Label _trust = LabelPresets.GetDefault(StrConversions.Percent((float)__instance.City.Trust.GetPercent(player)), scene.Engine);
        if (__instance.City.Trust.Dominated == player) _trust.Color = LabelPresets.Color_positive;
        InsertLabel(6, _trust);

        // 7 MODDED infrastructure
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


    [HarmonyPatch("GetTooltip"), HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ExplorerCity_GetTooltip_Reverse(ExplorerCity __instance, GameScene scene) =>
        throw new NotImplementedException("ERROR. ExplorerCity_GetTooltip_Reverse");


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
