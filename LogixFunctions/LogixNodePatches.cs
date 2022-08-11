using BaseX;
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
    [HarmonyPatch(typeof(LogixNode))]
    internal static class LogixNodePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LogixNode.GenerateVisual))]
        private static bool GenerateVisualPrefix(LogixNode __instance, ref bool ____generatingVisual, CleanupRef<Slot> ____activeVisual)
        {
            if (__instance.IsDestroyed || ____generatingVisual || ____activeVisual.Target != null)
                return false;

            var traverse = Traverse.Create(__instance);
            __instance.StartUnpackingWithLogixFunctions();

            try
            {
                ____generatingVisual = true;

                if (traverse.Property<bool>("Grabbable").Value)
                {
                    var grabbable = __instance.Slot.GetComponentOrAttach<Grabbable>(out _, null);
                    grabbable.Scalable.Value = true;
                    grabbable.ReparentOnRelease.Value = true;
                }

                foreach (IInputElement nodeInput in traverse.Property<EnumerableWrapper<IInputElement, LogixNode.InputEnumerator>>("Inputs").Value)
                {
                    if (nodeInput.TargetNode is LogixNode targetNode)
                        targetNode.GenerateVisual();
                }

                if (__instance.ShouldUnpackAsFunction())
                    __instance.GenerateLogixFunctionVisual();
                else
                {
                    var visual = __instance.Slot.AddSlot("Visual");
                    ____activeVisual.Target = visual;

                    if (!__instance.Enabled)
                        visual.Tag = "Disabled";

                    traverse.Method("OnGenerateVisual", visual).GetValue();
                }

                foreach (IOutputElement nodeOutput in traverse.Property<EnumerableWrapper<IOutputElement, LogixNode.OutputEnumerator>>("Outputs").Value)
                {
                    foreach (IInputElement connectedInput in nodeOutput.ConnectedInputs)
                    {
                        connectedInput.OwnerNode.GenerateVisual();
                    }
                }

                var referenceNodesToThis = Pool.BorrowList<IReferenceNode>();
                __instance.Slot.GetLogixReferences(null, __instance, referenceNodesToThis);

                foreach (IReferenceNode referenceNode in referenceNodesToThis)
                {
                    ((LogixNode)referenceNode).GenerateVisual();
                }

                Pool.Return(ref referenceNodesToThis);
            }
            finally
            {
                ____generatingVisual = false;

                __instance.EndPackingWithLogixFunctions();
            }

            return false;
        }
    }
}