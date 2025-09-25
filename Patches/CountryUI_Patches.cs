using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STM.UI.Floating;
using STMG.Engine;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(CountryUI))]
public static class CountryUI_Patches
{
    [HarmonyPatch("GetInfo"), HarmonyPrefix]
    public static bool GetInfo(CountryUI __instance)
    {
        int _height = MainData.Size_button * 7 + MainData.Margin_content_items * 2;
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, _height, 1f), 1, 8, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(MainData.Margin_content, 0f);
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "AddControl", [_grid, "info"]);

        CompanyUI.AddStats(_grid, 0, Localization.GetCity("capital"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetCapital", []), __instance.Scene, strong: true);
        CompanyUI.AddStats(_grid, 1, Localization.GetCity("cities"), () => StrConversions.CleanNumber(__instance.Country.Cities.Length), __instance.Scene);
        // No of not connected cities
        CompanyUI.AddStats(_grid, 2, "Not connected", () => StrConversions.CleanNumber(__instance.Country.Cities.Length - __instance.Country.GetConnectedCities()), __instance.Scene);
        CompanyUI.AddStats(_grid, 3, Localization.GetCity("level"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetLevel", []), __instance.Scene);
        // Average city level
        CompanyUI.AddStats(_grid, 4, "Average", () => { return StrConversions.CleanNumber((float)__instance.Country.GetLevel() / (float)__instance.Country.Cities.Length); },__instance.Scene);
        CompanyUI.AddStats(_grid, 5, Localization.GetCity("country_trust"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetTrust", []), __instance.Scene); // 3 => 4

        // Vehicle companies with the lowest import tax
        var Road = FindBestVehicleCompany(__instance.Country.GetCapital(0).User, "road_vehicle", __instance.Scene);
        var Train = FindBestVehicleCompany(__instance.Country.GetCapital(0).User, "train", __instance.Scene);
        CompanyUI.AddStats(_grid, 6, "<!cicon_road_vehicle>" + Road.Item2, () => StrConversions.PercentChange((float)Road.Item1), __instance.Scene);
        CompanyUI.AddStats(_grid, 7, "<!cicon_train>" + Train.Item2, () => StrConversions.PercentChange((float)Train.Item1), __instance.Scene);

        return false; // skip the original
    }


    /// <summary>
    /// Iterates through vehicle entity companies and finds the lowest import tax for a given  <paramref name="city"/>.
    /// <paramref name="type_name"></paramref> Either train  or road_vehicle.
    /// </summary>
    public static (decimal, string) FindBestVehicleCompany(this CityUser city, string type_name, GameScene scene)
    {
        Country country = city.City.GetCountry(scene);
        //Log.Write($"{city.Name} {country.Name}");

        // iterate through companies and calculate import tax
        var items = new List<(decimal Tax, string Name)>();
        foreach (VehicleCompanyEntity vce in MainData.Vehicle_companies.Where(x => x.Vehicles.Last.Type_name == type_name))
        {
            //Log.Write($"{vce.ID} {vce.Translated_name} {vce.Vehicles.Last.Type_name} {vce.Country.Item.ISO3166_1} reg={vce.Region.X}");
            Country company = vce.Country.Item;
            decimal distance = VehicleBaseEntity.GetHorizontalDistance(country.Location, company.Location) / 100m;
            decimal taxDistance = ((decimal)(int)(distance * 20m) / 20m * MainData.Defaults.Vehicles_import);
            items.Add((taxDistance, $"{vce.Translated_name} ({company.Name.GetTranslation(Localization.Language)})"));
            //Log.Write($" ... {company.Name} dist={distance} tax={taxDistance}");
        }

        return items.OrderBy(item => item.Tax).First(); // find the lowest tax
    }


    [HarmonyPatch("AddCity"), HarmonyPrefix]
    public static bool CountryUI_AddCity_Prefix(CountryUI __instance, CityUser city, ref int y)
    {
        if (city.Destinations == null || city.Level <= 0)
        {
            return false;
        }

        ControlCollection _content;
        Button _button = ButtonPresets.GetBlack(new ContentRectangle(0f, y, 0f, MainData.Size_button, 1f), __instance.Scene.Engine, out _content);
        _button.horizontal_alignment = HorizontalAlignment.Stretch;
        _button.Margin_local = new FloatSpace(0f, MainData.Margin_content_items, MainData.Margin_content, MainData.Margin_content_items);
        ExtensionsHelper.GetPrivateField<ControlCollection>(__instance, "cities").Transfer(_button);
        y += (int)_button.Size_local_total.Y;

        string _text = city.GetNameWithIcon(__instance.Scene);
        _text = (city.City.Capital ? ("<!cicon_train_b> " + _text) : ((!city.Important) ? ("<!cicon_ship_b> " + _text) : ("<!cicon_plane_b> " + _text)));
        Label _name = LabelPresets.GetBold(_text, __instance.Scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        _content.Transfer(_name);
        if (!city.Important)
        {
            _name.Font = MainData.Font_default.Get(__instance.Scene.Engine);
        }

        Label _level = LabelPresets.GetDefault("<!cicon_star> " + StrConversions.CleanNumber(city.Level) + (city.Routes.Count>0 ? "<!cicon_locate>" : ""), __instance.Scene.Engine);
        _level.horizontal_alignment = HorizontalAlignment.Right;
        _level.Margin_local = new FloatSpace(MainData.Margin_content);
        _content.Transfer(_level);

        _button.OnButtonPress += (Action)delegate
        {
            if (__instance.Scene.tracking != city)
            {
                __instance.Scene.tracking = city;
            }
            else if (!__instance.Scene.Selection.IsSelected(city))
            {
                city.Select(__instance.Scene);
            }
        };

        _button.OnMouseStillTime += (Action)delegate
        {
            ExtensionsHelper.CallPrivateMethodVoid(__instance, "GetCityTooltip", [_button, city]);
        };

        return false;
    }
}
