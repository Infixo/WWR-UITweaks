using HarmonyLib;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI.Floating;
using STMG.UI.Control;
using Utilities;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(BaseVehicleUI))]
public static class BaseVehicleUI_Patches
{
    [HarmonyPatch(typeof(BaseVehicleUI), MethodType.Constructor, [typeof(VehicleBaseUser), typeof(GameScene)]), HarmonyPostfix]
    public static void BaseVehicleUI_BaseVehicleUI_Postfix(BaseVehicleUI __instance, VehicleBaseUser vehicle, GameScene scene)
    {
        if (vehicle.Company != scene.Session.Player) return;
        // Add line number to the vehicle name
        LongText header = __instance.GetPrivateProperty<LongText>("Label_header");
        header.Text += " [" + ((vehicle.GetLine(scene)?.ID+1) ?? 0).ToString() + "]";
    }
}
