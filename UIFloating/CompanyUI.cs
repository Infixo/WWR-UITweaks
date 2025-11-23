using HarmonyLib;
using STM.GameWorld;
using STM.UI.Floating;
using STMG.UI.Control;
using Utilities;

namespace UITweaks.UIFloating;

[HarmonyPatch]
public static class CompanyUI_Patches
{
    [HarmonyPatch(typeof(CompanyUI), MethodType.Constructor, [typeof(Company), typeof(GameScene)]), HarmonyPostfix]
    public static void CompanyUI_CompanyUI_Postfix(CompanyUI __instance, Company company, GameScene scene)
    {
        // Add ID number to the company name
        LongText header = __instance.GetPrivateProperty<LongText>("Label_header");
        header.Text += " [" + company.ID.ToString() + "]";
    }
}
