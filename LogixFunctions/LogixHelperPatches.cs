using FrooxEngine;
using FrooxEngine.LogiX;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogixFunctions
{
    [HarmonyPatch(typeof(LogixHelper))]
    internal static class LogixHelperPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LogixHelper.GetConnectionPoint))]
        private static bool GetConnectionPointPrefix(IWorldElement element, string methodName, ref Slot __result)
        {
            if (element is DynamicImpulseTarget dynamicImpulseTarget && dynamicImpulseTarget.OwnerNode.ShouldUnpackAsFunction())
            {
                __result = dynamicImpulseTarget.GetLogixFunctionConnectionPoint();
                return false;
            }

            if (methodName != null && element is LogixNode logixNode && logixNode.ShouldUnpackAsFunction())
            {
                __result = logixNode.GetLogixFunctionConnectionPoint(methodName);
                return false;
            }

            // Don't care about interface targets

            if (element is IConnectionElement connectionElement && connectionElement.OwnerNode.ShouldUnpackAsFunction())
            {
                __result = connectionElement.GetLogixFunctionConnectionPoint();
                return false;
            }

            if (element is Impulse impulse && impulse.OwnerNode.ShouldUnpackAsFunction())
            {
                __result = impulse.GetLogixFunctionConnectionPoint();
                return false;
            }

            // Don't care about interface targets
            // Use existing method for non-function targets
            return true;
        }
    }
}