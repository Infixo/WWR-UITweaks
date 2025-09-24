using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.Engine;
using STMG.UI.Control;
using STVisual.Utility;
using static System.Formats.Asn1.AsnWriter;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(CountryUI))]
public static class CountryUI_Patches
{
    [HarmonyPatch("GetInfo"), HarmonyPrefix]
    public static bool GetInfo(CountryUI __instance)
    {
        int _height = MainData.Size_button * 6 + MainData.Margin_content_items * 2;
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, _height, 1f), 1, 7, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(MainData.Margin_content, 0f);
        ExtensionsHelper.CallPrivateMethodVoid(__instance, "AddControl", [_grid, "info"]);

        CompanyUI.AddStats(_grid, 0, Localization.GetCity("capital"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetCapital", []), __instance.Scene, strong: true);
        CompanyUI.AddStats(_grid, 1, Localization.GetCity("cities"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetCities", []), __instance.Scene);
        CompanyUI.AddStats(_grid, 2, Localization.GetCity("level"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetLevel", []), __instance.Scene);
        // Average city level
        CompanyUI.AddStats(_grid, 3, "Average", () => { return StrConversions.CleanNumber((float)__instance.Country.GetLevel() / (float)__instance.Country.Cities.Length); },__instance.Scene);
        CompanyUI.AddStats(_grid, 4, Localization.GetCity("country_trust"), () => ExtensionsHelper.CallPrivateMethod<string>(__instance, "GetTrust", []), __instance.Scene); // 3 => 4

        // Vehicle companies with the lowest import tax
        var Road = FindBestVehicleCompany(__instance.Country.GetCapital(0).User, "road_vehicle", __instance.Scene);
        var Train = FindBestVehicleCompany(__instance.Country.GetCapital(0).User, "train", __instance.Scene);
        CompanyUI.AddStats(_grid, 5, "<!cicon_road_vehicle>" + Road.Item2, () => StrConversions.PercentChange((float)Road.Item1), __instance.Scene);
        CompanyUI.AddStats(_grid, 6, "<!cicon_train>" + Train.Item2, () => StrConversions.PercentChange((float)Train.Item1), __instance.Scene);

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
}
