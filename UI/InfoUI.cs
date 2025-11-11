using HarmonyLib;
using STM.GameWorld;
using STM.UI;
using STMG.UI.Control;

namespace UITweaks.UI;


[HarmonyPatch(typeof(InfoUI))]
public static class InfoUI_Patches
{
    internal static bool NoCapex = true;

    // Display Operating Profit instead of current balance
    [HarmonyPatch("Update"), HarmonyPrefix]
    public static bool InfoUI_Update_Prefix(InfoUI __instance, Label ___wealth, Label ___balance, Session ___Session)
    {
        Company _player = __instance.Company;
        long _balance = NoCapex ? _player.GetCurrentMonthBalance() : _player.Balance.GetCurrentMonth();
        ___wealth.Text = StrConversions.GetBalance(_player.Wealth, ___Session.Scene.currency);
        ___wealth.Color = _player.Wealth < 0 ? LabelPresets.Color_negative : LabelPresets.Color_main;
        ___balance.Text = StrConversions.GetBalanceWithPlus(_balance, ___Session.Scene.currency);
        ___balance.Color = _balance < 0 ? LabelPresets.Color_negative : LabelPresets.Color_positive;
        return false;
    }
}
