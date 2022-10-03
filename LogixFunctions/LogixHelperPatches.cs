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
    [HarmonyPatch(typeof(LogixHelper))]
    internal static class LogixHelperPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LogixHelper.GetConnectionPoint))]
        private static bool GetConnectionPointPrefix(IWorldElement element, string methodName, ref Slot __result)
        {
            if (element is DynamicImpulseTarget dynamicImpulseTarget && dynamicImpulseTarget.OwnerNode.IsUnpackedAsLogixFunction())
            {
                __result = dynamicImpulseTarget.GetLogixFunctionConnectionPoint();
                return false;
            }

            if (methodName != null && element is LogixNode logixNode && logixNode.IsUnpackedAsLogixFunction())
            {
                __result = logixNode.GetLogixFunctionConnectionPoint(methodName);
                return false;
            }

            // Don't care about interface targets

            if (element is IConnectionElement connectionElement && connectionElement.OwnerNode.IsUnpackedAsLogixFunction())
            {
                __result = connectionElement.GetLogixFunctionConnectionPoint();
                return false;
            }

            if (element is Impulse impulse && impulse.OwnerNode.IsUnpackedAsLogixFunction())
            {
                __result = impulse.GetLogixFunctionConnectionPoint();
                return false;
            }

            // Don't care about interface targets
            // Use existing method for non-function targets
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(LogixHelper.MoveUnder), typeof(LogixNode), typeof(Slot), typeof(bool), typeof(LogixTraversal))]
        private static bool MoveUnderPrefix(LogixNode node, Slot under, bool addSelf, LogixTraversal traversal)
        {
            var slotsToMove = Pool.BorrowHashSet<Slot>();

            if (addSelf && node.ShouldMoveWithPack(under))
                slotsToMove.Add(node.Slot);

            foreach (var reachableNode in node.EnumerateAllReachableNodes(null, traversal))
                if (reachableNode.ShouldMoveWithPack(under))
                    slotsToMove.Add(reachableNode.Slot);
                else
                    slotsToMove.Add(reachableNode.FindLogixFunctionRoot());

            foreach (var slot in slotsToMove)
                if (slot.IsRemoved)
                    UniLog.Warning("Removed Node when enumerating reachable nodes on " + node.ParentHierarchyToString() + ". Removed Node Slot: " + slot.ParentHierarchyToString(), false);
                else
                    slot.SetParent(under, true);

            Pool.Return(ref slotsToMove);

            return false;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(nameof(LogixHelper.EnumerateAllReachableNodes))]
    }
}