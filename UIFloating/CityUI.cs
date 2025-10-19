using HarmonyLib;
using Utilities;
using STM.Data;
using STM.GameWorld;
using STM.UI;
using STM.UI.Floating;
using STMG.UI.Control;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(CityUI))]
public static class CityUI_Patches
{
    [HarmonyPatch("GetTravelerTooltip"), HarmonyPrefix]
    public static bool CityUI_GetTravelerTooltip_Prefix(CityUI __instance, CityUI.TravelersDest destination)
    {
        TooltipPreset _tooltip = GeneralTooltips.GetPassengers(__instance.Scene.Engine);
        __instance.AttachLines(_tooltip, __instance.City, destination.Destination, __instance.Scene);
        _tooltip.AddSeparator();
        _tooltip.AddStatsLine(Localization.GetGeneral("passengers"), () => "<!cicon_passenger> " + StrConversions.CleanNumber(destination.Total()));
        _tooltip.AddStatsLine(Localization.GetCity("direct"), () => "<!cicon_passenger> " + StrConversions.CleanNumber(destination.direct), alt: true, 1);
        _tooltip.AddStatsLine(Localization.GetCity("connecting"), () => "<!cicon_passenger> " + StrConversions.CleanNumber(destination.indirect), alt: true, 1);
        _tooltip.AddStatsLine(Localization.GetCity("returning"), () => "<!cicon_passenger> " + StrConversions.CleanNumber(destination.going_home), alt: true, 1);
        if (__instance.City.Accepts_indirect > 20)
        {
            _tooltip.AddSpace();
            _tooltip.AddDescription("<!cicon_error:32>" + Localization.GetInfo("city_no_indirect")).Color = LabelPresets.Color_negative;
            _tooltip.AddStatsLine(Localization.GetCity("connecting"), () => StrConversions.OutOf(__instance.City.GetTotalIndirect(), __instance.City.GetMaxIndirect()), alt: true, 1);
        }
        if (destination.indirect != 0)
        {
            _tooltip.AddSeparator();
            _tooltip.AddBoldLabel(Localization.GetCity("connecting"), null, center: true);
            _tooltip.AddSpace();
            bool _alt = false;
            var matches = __instance.City.Indirect.ToArray().Where(x => x.Destination == destination.Destination.City).OrderByDescending(x => x.People);
            foreach (Passengers _item in matches)
            {
                string _text = _item.Start.User.GetNameWithIcon(__instance.Scene);
                _tooltip.AddStatsLine(_text, () => StrConversions.CleanNumber(_item.People), _alt);
                _alt = !_alt;
            }
        }
        __instance.CallPrivateMethodTypesVoid("SetUpArrows", [typeof(CityUI.TravelersDest), typeof(IControl)], [destination, _tooltip.Main_control]);
        _tooltip.AddToControlAuto(destination.Control);
        return false;
    }
}
