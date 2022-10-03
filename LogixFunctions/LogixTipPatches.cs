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
    [HarmonyPatch(typeof(LogixTip))]
    internal static class LogixTipPatches
    {
        [HarmonyReversePatch]
        [HarmonyPatch("GetHeldSlotReference")]
        private static Slot GetHeldSlotReference(LogixTip instance, out ReferenceProxy referenceProxy)
        {
            throw new NotImplementedException("This should get patched in by Harmony!");
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnRevealNodes")]
        private static void OnRevealNodesPostfix(Slot __state)
        {
            __state.EndUnpackingWithLogixFunctions();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnRevealNodes")]
        private static void OnRevealNodesPrefix(LogixTip __instance, ref Slot __state)
        {
            __state = GetHeldSlotReference(__instance, out _);
            __state.StartUnpackingWithLogixFunctions();
        }
    }
}