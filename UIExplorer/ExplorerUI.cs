using HarmonyLib;
using STM.GameWorld;
using STM.UI.Explorer;
using STMG.UI.Control;
using Utilities;

namespace UITweaks.UIExplorer;


[HarmonyPatch(typeof(ExplorerUI<IExplorerItem>))]
public static class ExplorerUI_Patches
{
    [HarmonyPatch("AddItem"), HarmonyPostfix]
    public static void ExplorerUI_AddItem_Postfix(ExplorerUI<IExplorerItem> __instance, IExplorerItem item, GameScene ___Scene)
    {
        // Clear original event and define a new one
        item.Main_control.OnButtonPress.Clear();
        item.Main_control.OnButtonPress += (Action)delegate
        {
            Delegate On_select = __instance.GetPrivateField<Delegate>("On_select");
            On_select.DynamicInvoke([item]);
            if (!___Scene.Engine.Keys.Ctrl && !___Scene.Engine.Keys.Shift)
                __instance.Close();
            // Get_selected is ALWAYS null atm, perhaps reserved for the future
            //else if (Get_selected != null)
            //{
                //ValidateSelection();
            //}
        };
    }


    // Longer search field (now it only 20 chars)
    [HarmonyPatch("GetTop"), HarmonyPostfix]
    public static void ExplorerUI_GetTop_Postfix(ExplorerUI<IExplorerItem> __instance, TextBlock ___search)
    {
        ___search.character_limit = 50;
    }


    [HarmonyPatch("ContainsText"), HarmonyPrefix]
    public static bool ExplorerUI_ContainsText_Prefix(ref bool __result, IExplorerItem item, string text)
    {
        __result = true;
        string[] keywords = text.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        // check labels
        for (int j = 0; j < item.Labels.Length; j++)
        {
            if (keywords.Any(k => item.Labels[j].Text.Contains(k, System.StringComparison.OrdinalIgnoreCase)))
                return false; // actually returns true - found
        }

        // check path for a vehicle
        if (item is ExplorerVehicleUser _user)
        {
            for (int i = 0; i < _user.User.Route.Instructions.Cities.Length; i++)
            {
                if (keywords.Any(k => _user.User.Route.Instructions.Cities[i].Name.Contains(k, System.StringComparison.OrdinalIgnoreCase)))
                    return false; // actually returns true - found
            }
        }

        __result = false;
        return false; // not found
    }
}
