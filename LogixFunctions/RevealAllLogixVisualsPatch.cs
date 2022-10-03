using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Meta;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogixFunctions
{
    [HarmonyPatch(typeof(RevealAllLogixVisuals))]
    internal static class RevealAllLogixVisualsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(RevealAllLogixVisuals.Reveal))]
        private static bool RevealPrefix(RevealAllLogixVisuals __instance)
        {
            if (!(__instance.SearchRoot.Evaluate(null) is Slot slot))
                return false;

            slot.StartUnpackingWithLogixFunctions();
            slot.GetComponentsInChildren<LogixNode>().ForEach(node => node.GenerateVisual());
            slot.EndUnpackingWithLogixFunctions();

            __instance.OnRevealed.Trigger();

            return false;
        }
    }
}