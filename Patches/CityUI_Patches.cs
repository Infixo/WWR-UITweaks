using HarmonyLib;
using Utilities;
using STM.Data;
using STM.GameWorld;
using STM.UI;
using STM.UI.Floating;
using STMG.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(CityUI))]
public static class CityUI_Patches
{
    [HarmonyPatch("GetTravelerTooltip"), HarmonyPrefix]
    public static bool CityUI_GetTravelerTooltip_Prefix(CityUI __instance, TravelersDest destination)
    {
        TooltipPreset _tooltip = GeneralTooltips.GetPassengers(__instance.Scene.Engine);
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
            for (int i = 0; i < __instance.City.Indirect.Count; i++)
            {
                if (__instance.City.Indirect[i].Destination == destination.Destination.City)
                {
                    Passengers _item = __instance.City.Indirect[i];
                    string _text = _item.Start.User.GetNameWithIcon(__instance.Scene);
                    _tooltip.AddStatsLine(_text, () => StrConversions.CleanNumber(_item.People), _alt);
                    _alt = !_alt;
                }
            }
        }
        __instance.CallPrivateMethodVoid("AttachLines", [_tooltip, __instance.City, destination.Destination, __instance.Scene]);
        __instance.CallPrivateMethodVoid("SetUpArrows", [destination, _tooltip.Main_control]);
        _tooltip.AddToControlAuto(destination.Control);
        return false;
    }
}
