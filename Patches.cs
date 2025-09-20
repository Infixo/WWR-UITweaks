using HarmonyLib;
using STM.GameWorld;
using STM.UI;
using STM.UI.Explorer;
using STMG.Engine;
using STMG.UI.Control;
using STMG.UI.Utility;

namespace TestMod;

[HarmonyPatch]
public static class Patches
{
    [HarmonyPatch(typeof(CreateNewRouteAction), "GenerateVehiclesSelection")]
    [HarmonyPatch([typeof(Action < ExplorerVehicleEntity >), typeof(Func<ExplorerVehicleEntity, bool>), typeof(NewRouteSettings), typeof(IControl), typeof(GameScene), typeof(bool), typeof(string), typeof(byte), typeof(long)])]
    [HarmonyPrefix]
    public static bool CreateNewRouteAction_GenerateVehiclesSelection_Prefix(Action<ExplorerVehicleEntity> on_select, Func<ExplorerVehicleEntity, bool> is_selected, NewRouteSettings route, IControl parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L)
    {

        {
            Log.Write($"name={route.Name}, cities={route.Cities.Count}, vehicle={route.vehicle}, history={history}");
            //__instance.World.GetOrCreateSystemManaged<RealPop.Systems.AgingSystem_RealPop>().Update();
            return true; // test: continue
        }
    }
}
