using HarmonyLib;
using Microsoft.Xna.Framework;
using ShoppingFix;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using System;
using System.Diagnostics;
using System.Reflection;
//using System.Reflection.Emit;

namespace UITweaks.Patches;


[HarmonyPatch(typeof(ExplorerVehicleEntity))]
public static class ExplorerVehicleEntity_Patches
{
    [HarmonyPatch(
        typeof(ExplorerVehicleEntity),
        MethodType.Constructor,
        [typeof(VehicleBaseEntity), typeof(GameScene), typeof(Company), typeof(CityUser), typeof(long), typeof(int)])]
    [HarmonyPostfix]
    public static void ExplorerVehicleEntity_Postfix(ExplorerVehicleEntity __instance, VehicleBaseEntity entity, GameScene scene, Company company, CityUser city, long price_adjust, int range = 0)
    {
        Log.Write($"{entity.Translated_name} comp={company.ID} inf={price_adjust} ran={range} {__instance.Labels[8].Text} {__instance.Labels[9].Text}");
        //Log.WriteCallingStack(10);
        //return true; // continue
        // MODDED
        //__instance.Labels[6].Text = "profit";
        //__instance.Labels[7].Text = "effic";
        //__instance.Labels[8].Text = "through";
        //__instance.Labels[9].Text = "range";
    }

    [HarmonyPatch("GetMainControl"), HarmonyPostfix]
    public static void GetMainControl(ExplorerVehicleEntity __instance, GameScene scene, Company company, CityUser city)
    {
        //Log.Write($"ORG {__instance.Labels.Length} {__instance.Labels[0]} {__instance.Labels[1]}");
        // Create a new, larger array
        Label[] newLabels = new Label[10];
        // Copy elements from the old array to the new one
        Array.Copy(__instance.Labels, newLabels, __instance.Labels.Length);
        //ExtensionsHelper.SetPrivateProperty(__instance, "Labels", newLabels); // __instance.Labels = newLabels;
        // damn... cannot use the above because only setter is private, getter is public
        // The property itself is public, so we need BindingFlags.Public.
        // However, the setter is non-public, so we'll access it separately.
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        Type type = __instance.GetType();
        PropertyInfo propertyInfo = type.GetProperty("Labels", flags);

        // Now, get the private setter method. We need to look for a non-public method.
        var setter = propertyInfo.GetSetMethod(true); // The 'true' argument is crucial.

        if (setter != null)
        {
            setter.Invoke(__instance, new object[] { newLabels });
        }
        //Log.Write($"EXT {__instance.Labels.Length} {__instance.Labels[0]} {__instance.Labels[1]}");

        // fill out new columns
        __instance.Labels[6] = LabelPresets.GetDefault("999999$", scene.Engine);
        __instance.Labels[7] = LabelPresets.GetDefault("99%", scene.Engine, Color.Lavender);
        __instance.Labels[8] = LabelPresets.GetBold("999", scene.Engine, Color.Red);
        __instance.Labels[9] = LabelPresets.GetBold("9999", scene.Engine);
    }

    [HarmonyPatch("Smaller"), HarmonyPrefix]
    public static bool ExplorerVehicleEntity_Smaller_Prefix(ExplorerVehicleEntity __instance, ref bool __result, IExplorerItem item, int sort_id)
    {
        Log.Write($"{__instance.Labels[0].Text} vs. {item.Labels[0].Text}");
        __result = Smaller_Modded(__instance, item, sort_id);
        return false; // skip original
    }

    // Worldwide Rush, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
    // STM.UI.Explorer.ExplorerVehicleEntity
    public static bool Smaller_Modded(ExplorerVehicleEntity __instance, IExplorerItem item, int sort_id)
    {
        ExplorerVehicleEntity _item = (ExplorerVehicleEntity)item;
        if (__instance.Valid != _item.Valid)
        {
            return __instance.Valid.CompareTo(_item.Valid) > 0;
        }
        //if (sort_id == 0)
        //{
            return __instance.Labels[0].Text.CompareTo(_item.Labels[0].Text) < 0;
        //}
        /* TEMP DISABLE 
        if (sort_id - __instance.Labels.Length == 0)
        {
            return __instance.Labels[0].Text.CompareTo(_item.Labels[0].Text) > 0;
        }
        if (sort_id == 1)
        {
            int _result7 = __instance.Labels[1].Text.CompareTo(_item.Labels[1].Text);
            if (_result7 == 0)
            {
                return BackupCompare(_item);
            }
            return _result7 < 0;
        }
        if (sort_id - Labels.Length == 1)
        {
            int _result10 = __instance.Labels[1].Text.CompareTo(_item.Labels[1].Text);
            if (_result10 == 0)
            {
                return BackupCompare(_item);
            }
            return _result10 > 0;
        }
        if (sort_id == 2)
        {
            int _result9 = capacity.CompareTo(_item.capacity);
            if (_result9 == 0)
            {
                return BackupCompare(_item);
            }
            return _result9 < 0;
        }
        if (sort_id - Labels.Length == 2)
        {
            int _result8 = capacity.CompareTo(_item.capacity);
            if (_result8 == 0)
            {
                return BackupCompare(_item);
            }
            return _result8 > 0;
        }
        if (sort_id == 3)
        {
            int _result6 = Entity.Speed.CompareTo(_item.Entity.Speed);
            if (_result6 == 0)
            {
                return BackupCompare(_item);
            }
            return _result6 < 0;
        }
        if (sort_id - __instance.Labels.Length == 3)
        {
            int _result5 = Entity.Speed.CompareTo(_item.Entity.Speed);
            if (_result5 == 0)
            {
                return BackupCompare(_item);
            }
            return _result5 > 0;
        }
        if (sort_id == 4)
        {
            int _result4 = stock.CompareTo(_item.stock);
            if (_result4 == 0)
            {
                return BackupCompare(_item);
            }
            return _result4 < 0;
        }
        if (sort_id - __instance.Labels.Length == 4)
        {
            int _result3 = stock.CompareTo(_item.stock);
            if (_result3 == 0)
            {
                return BackupCompare(_item);
            }
            return _result3 > 0;
        }
        if (sort_id == 5)
        {
            int _result2 = price.CompareTo(_item.price);
            if (_result2 == 0)
            {
                return BackupCompare(_item);
            }
            return _result2 < 0;
        }
        if (sort_id - __instance.Labels.Length == 5)
        {
            int _result = price.CompareTo(_item.price);
            if (_result == 0)
            {
                return BackupCompare(_item);
            }
            return _result > 0;
        }
        return false;
        */
    }


}

/*
[HarmonyPatch(typeof(CreateNewRouteAction))]
public static class CreateNewRouteAction_Patches
{
    [HarmonyPatch("GenerateVehiclesSelection"),
        [typeof(Action<ExplorerVehicleEntity>), typeof(Func<ExplorerVehicleEntity, bool>), typeof(NewRouteSettings), typeof(IControl) parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L)
        [HarmonyPrefix]
    public static bool CreateNewRouteAction_GenerateVehiclesSelection_Prefix(CreateNewRouteAction __instance, Action<ExplorerVehicleEntity> on_select, Func<ExplorerVehicleEntity, bool> is_selected, NewRouteSettings route, IControl parent, GameScene scene, bool above, string history, byte type = byte.MaxValue, long price_adjust = 0L)
    {
        Log.Write($" route={route.Name} cities={route.Cities.Count} price={price_adjust} typ={type}");
        //Log.WriteCallingStack(10);
        return true; // continue
    }

}
*/