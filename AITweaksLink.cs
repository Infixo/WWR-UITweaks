using STM.GameWorld;
using STM.UI;
using STMG.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace UITweaks;

internal static class AITweaksLink
{
    private static bool _AITweaksActive = false;
    private static MethodInfo? _GetEvaluationTooltip;
    private static MethodInfo? _GetNumEvaluations;

    internal static bool Active { get => _AITweaksActive; }

    internal static TooltipPreset GetEvaluationTooltip(Line line, GameEngine engine)
    {
        object? tooltipObject = _GetEvaluationTooltip?.Invoke(null, [line, engine]); // null because we call a static method, no object to call on
        TooltipPreset tooltip = tooltipObject as TooltipPreset ?? TooltipPreset.Get("ERROR", engine, can_lock: true);
        return tooltip!;
    }


    internal static int GetNumEvaluations(Line line)
    {
        if (!_AITweaksActive) return -1;
        object? numObject = _GetNumEvaluations?.Invoke(null, [line]); // null because we call a static method, no object to call on
        int numEvals = numObject as int? ?? -1;
        return numEvals;
    }


    internal static void InitAITweaks()
    {
        // See if AITweaks is loaded, so we can use Evaluation Tooltip
        Assembly? asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => (a.GetName().Name ?? "unknown").Equals("AITweaks", System.StringComparison.OrdinalIgnoreCase));
        if (asm == null)
        {
            Log.Write("AITweaks not loaded. Cannot use Evaluation Tooltip.");
            _AITweaksActive = false;
            return;
        }
        Log.Write("Found AITweaks: " + asm.FullName);

        // Get methods
        Type? bridgeType = asm?.GetType("AITweaks.Patches.LineEvaluation");
        _GetEvaluationTooltip = bridgeType?.GetMethod("GetEvaluationTooltip", BindingFlags.Public | BindingFlags.Static);
        _GetNumEvaluations = bridgeType?.GetMethod("GetNumEvaluations", BindingFlags.Public | BindingFlags.Static);
        if (_GetEvaluationTooltip == null || _GetNumEvaluations == null)
        {
            Log.Write("Cannot access AITweaks method(s). Cannot use Evaluation Tooltip.");
            _AITweaksActive = false;
            return;
        }

        // Successful link
        Log.Write("Found method: " + bridgeType?.FullName + " " + _GetEvaluationTooltip.Name);
        Log.Write("Found method: " + bridgeType?.FullName + " " + _GetNumEvaluations.Name);
        _AITweaksActive = true;
    }
}
