using System.Runtime.InteropServices;
using Utilities;

namespace UITweaks;


public static class ModEntry
{
    public static readonly string ModName = nameof(UITweaks);

    [UnmanagedCallersOnly]
    public static int InitializeMod()
    {
        if (ModInit.InitializeMod(ModName))
        {
            // do other stuff here to initialize
            //CityWorldGraphics_Patches.DebugColors();
            AITweaksLink.InitAITweaks();
        }
        return 0;
    }
}
