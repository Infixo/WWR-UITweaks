using HarmonyLib;
using STM;
using STM.GameWorld;
using STM.GameWorld.Users;
using STMG.Utility;
using Utilities;

namespace UITweaks.GameWorld;


// Set of patches to add Hub info to the upgrade settings passed to CommandNewRoute and others
[HarmonyPatch(typeof(UpgradeSettings))]
public static class UpgradeSettings_Patches
{
    [HarmonyPatch(typeof(UpgradeSettings), MethodType.Constructor, [typeof(VehicleBaseUser), typeof(GameScene)]), HarmonyPostfix]
    public static void UpgradeSettings_UpgradeSettings(UpgradeSettings __instance, VehicleBaseUser vehicle, GameScene scene)
    {
        SavingHeader _header = new SavingHeader(Main.Version_data, Main.Version_game, scene.Demo);
        MultiPartSavingStream _stream = new MultiPartSavingStream();
        vehicle.GetUpgradeData(_header, _stream.Add("vehicle", compressed: true));
        _header.GetData(_stream.Add("header", compressed: true));
        // patch for hub
        SavingStream _hub_stream = _stream.Add("hub", compressed: true);
        _hub_stream += vehicle.Hub.City;
        // end of patch
        MemoryStream _memory = new();
        _stream.Write(_memory, "h");
        __instance.SetPrivateField("data", _memory.ToArray());
        _memory.Dispose();
    }

    public static Hub? GetHub(this UpgradeSettings settings, ushort company, GameScene scene)
    {
        MemoryStream memoryStream = new MemoryStream(settings.GetPrivateField<byte[]>("data"));
        MultiPartReadingStream multiPartReadingStream = MultiPartReadingStream.Get(memoryStream, "h");
        ReadingStream? _hub_stream = multiPartReadingStream.GetPart("hub");
        Hub? hub = null;
        if (_hub_stream != null)
        {
            ushort _hub_city = _hub_stream;
            hub = scene.Cities[_hub_city].User.GetHub(company);
        }
        memoryStream.Dispose();
        return hub;
    }
}
