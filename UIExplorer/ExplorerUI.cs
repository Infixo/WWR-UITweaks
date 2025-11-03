using HarmonyLib;
using STM.GameWorld;
using STM.UI.Explorer;
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
}
