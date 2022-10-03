using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogixFunctions
{
    internal static class LogixFunction
    {
        public const string LogixFunctionTag = "LogixFunction";
        public const string LogixFunctionVisualName = "Visual";
        private const string ConnectPointName = "ConnectPoint";
        private const float NodeBaseWidth = 64;
        private const float NodeVerticalPadding = 8;
        private static bool currentlyUnpacking = false;
        private static Slot unpackRoot = null;

        public static void EndUnpackingWithLogixFunctions(this Slot unpackStart)
        {
            currentlyUnpacking = false;
            unpackRoot = null;
        }

        public static Slot FindLogixFunctionRoot(this LogixNode logixNode)
        {
            return logixNode.Slot.FindLogixFunctionRoot();
        }

        public static Slot FindLogixFunctionRoot(this Slot slot)
        {
            return slot.Tag == LogixFunctionTag ? slot : slot.FindParent(parent => parent.Tag == LogixFunctionTag);
        }

        public static Slot GenerateLogixFunctionVisual(this Slot logixFunctionRoot)
        {
            if (logixFunctionRoot.Tag != LogixFunctionTag)
                return null;

            var visual = logixFunctionRoot.Find(LogixFunctionVisualName);
            if (visual != null)
                return visual;

            var grabbable = logixFunctionRoot.GetComponentOrAttach<Grabbable>(out _);
            grabbable.Scalable.Value = true;
            grabbable.ReparentOnRelease.Value = true;

            // Generate
            var function = new Function(logixFunctionRoot);
            visual = logixFunctionRoot.AddSlot(LogixFunctionVisualName);
            if (!logixFunctionRoot.ActiveSelf)
                visual.Tag = "Disabled";

            var canvas = visual.AddSlot("Canvas");

            var width = NodeBaseWidth + 16;
            var height = function.Height + 2 * NodeVerticalPadding;
            var size = new float2(width, height);

            var extraInputPadding = 0.5f * (height - function.InputsHeight);
            var extraOutputPadding = 0.5f * (height - function.OutputsHeight);

            var inputYPos = extraInputPadding + 32f;
            var outputYPos = extraOutputPadding + 32f;

            var builder = new UIBuilder(canvas, width, height, 0.001f);
            builder.Canvas.AcceptPhysicalTouch.Value = false;

            var color = function.LogixNodes.Any(node => !node.Enabled) ? LogixNode.DEFAULT_NODE_ERROR_BACKGROUND : LogixNode.DEFAULT_NODE_BACKGROUND;
            builder.Panel(color, true);

            ConnectPoint connectPoint;
            foreach (var impulseTargetInfo in function.ImpulseTargets)
            {
                connectPoint = builder.GenerateConnectPoint(size, ref inputYPos, typeof(Action), false, false, false);
                connectPoint.Proxy.AttachComponent<ImpulseTargetProxy>(true, null).ImpulseTarget.Target = impulseTargetInfo.Method;
            }

            foreach (var inputElement in function.InputElements)
            {
                if (inputElement.GetSide() == ConnectPointSide.Input)
                    connectPoint = builder.GenerateConnectPoint(size, ref inputYPos, inputElement.InputType, false, false, true);
                else
                    connectPoint = builder.GenerateConnectPoint(size, ref outputYPos, inputElement.InputType, true, false, true);

                connectPoint.Proxy.AttachComponent<InputProxy>(true, null).InputField.Target = inputElement;
                connectPoint.Wire.InputField.Target = inputElement;
            }

            foreach (var impulseSource in function.ImpulseSources)
            {
                connectPoint = builder.GenerateConnectPoint(size, ref outputYPos, typeof(Action), true, true, true);

                connectPoint.Proxy.AttachComponent<ImpulseSourceProxy>(true, null).ImpulseSource.Target = impulseSource;
                connectPoint.Wire.InputField.Target = impulseSource;
            }

            foreach (var outputElement in function.OutputElements)
            {
                if (outputElement.GetSide() == ConnectPointSide.Output)
                    connectPoint = builder.GenerateConnectPoint(size, ref outputYPos, outputElement.OutputType, true, true, false);
                else
                    connectPoint = builder.GenerateConnectPoint(size, ref inputYPos, outputElement.OutputType, false, true, false);

                connectPoint.Proxy.AttachComponent<OutputProxy>(true, null).OutputField.Target = outputElement;
            }

            foreach (var logixNode in function.OtherConnectedNodes)
                logixNode.GenerateVisual();

            builder.VerticalLayout(0f, NodeVerticalPadding, null);
            builder.Text(logixFunctionRoot.Name).AutoSizeMin.Value = 0f;

            return visual;
        }

        public static Slot GenerateLogixFunctionVisual(this LogixNode logixNode)
        {
            return logixNode.FindLogixFunctionRoot()?.GenerateLogixFunctionVisual();
        }

        public static Slot GetLogixFunctionConnectionPoint(this IConnectionElement connectionElement)
        {
            var visual = connectionElement.OwnerNode.GenerateLogixFunctionVisual();

            if (connectionElement is IInputElement inputElement)
            {
                var inputProxy = visual.GetComponentInChildren<InputProxy>(proxy => proxy.InputField.Target == inputElement);
                return inputProxy?.Slot.Find(ConnectPointName);
            }

            if (connectionElement is IOutputElement outputElement)
            {
                var outputProxy = visual.GetComponentInChildren<OutputProxy>(proxy => proxy.OutputField.Target == outputElement);
                return outputProxy?.Slot.Find(ConnectPointName);
            }

            return null;
        }

        public static Slot GetLogixFunctionConnectionPoint(this DynamicImpulseTarget dynamicImpulseTarget)
        {
            var visual = dynamicImpulseTarget.OwnerNode.GenerateLogixFunctionVisual();

            var impulseTargetProxy = visual.GetComponentInChildren<ImpulseTargetProxy>(proxy => proxy.ImpulseTarget.Target.Target == dynamicImpulseTarget);
            return impulseTargetProxy?.Slot.Find(ConnectPointName);
        }

        public static Slot GetLogixFunctionConnectionPoint(this Impulse impulse)
        {
            var visual = impulse.OwnerNode.GenerateLogixFunctionVisual();

            var impulseSourceProxy = visual.GetComponentInChildren<ImpulseSourceProxy>(proxy => proxy.ImpulseSource.Target == impulse);
            return impulseSourceProxy?.Slot.Find(ConnectPointName);
        }

        public static Slot GetLogixFunctionConnectionPoint(this LogixNode logixNode, string methodName)
        {
            var visual = logixNode.GenerateLogixFunctionVisual();

            var impulseTargetProxy = visual.GetComponentInChildren<ImpulseTargetProxy>(proxy => proxy.ImpulseTarget.Target.Method.Name == methodName);
            return impulseTargetProxy?.Slot.Find(ConnectPointName);
        }

        public static bool IsUnpackedAsLogixFunction(this LogixNode logixNode)
        {
            return logixNode.Slot.FindLogixFunctionRoot()?.Find(LogixFunctionVisualName) != null;
        }

        public static bool ShouldMoveWithPack(this LogixNode logixNode, Slot packRoot)
        {
            var nodeFunctionRoot = logixNode.FindLogixFunctionRoot();

            return nodeFunctionRoot == null || packRoot.IsChildOf(nodeFunctionRoot, true);
        }

        public static bool ShouldUnpackAsFunction(this LogixNode logixNode)
        {
            // Unpack as function node, when the node has a function root, and it's (under) the unpacking root
            return logixNode.IsUnpackedAsLogixFunction() || (logixNode.FindLogixFunctionRoot() is Slot functionRoot && !(unpackRoot?.IsChildOf(functionRoot) ?? true));
        }

        public static void StartUnpackingWithLogixFunctions(this Slot packRoot)
        {
            currentlyUnpacking = true;
            unpackRoot = packRoot;
        }

        private static ConnectPoint GenerateConnectPoint(this UIBuilder ui, float2 size, ref float yPos, Type type, bool outputSide, bool isOutput, bool genWire)
        {
            var isImpulse = typeof(Delegate).IsAssignableFrom(type);
            var connectorSprite = LogixHelper.GetConnectorSprite(ui.Root.World, type.GetDimensions(), outputSide, isImpulse);

            var color = type.GetColor();
            color = color.MulA(0.8f);
            var image = ui.Image(connectorSprite, color);

            var imageRect = outputSide ? new Rect(size.x - 8f, -yPos, 8f, 32f) : new Rect(0f, -yPos, 8f, 32f);
            var rectTransform = image.RectTransform;
            var anchor = new float2(0f, 1f);
            rectTransform.SetFixedRect(imageRect, anchor);
            yPos += 36f;
            Slot slot = image.Slot;
            slot.LocalScale = float3.One / 0.001f;
            slot.AttachComponent<RectSlotDriver>();
            anchor = (imageRect.size) * 0.001f;
            slot.AttachComponent<BoxCollider>().Size.Value = new float3(anchor, 0.01f);

            var connectPointSlot = slot.AddSlot(ConnectPointName);
            connectPointSlot.LocalPosition = 0.004f * (outputSide ? float3.Right : float3.Left);

            ConnectionWire wire = null;
            if (genWire)
            {
                wire = connectPointSlot.AttachComponent<ConnectionWire>();
                wire.SetupType(type);

                if (outputSide)
                    wire.SetupAsOutput(isOutput);
            }

            return new ConnectPoint(slot, wire);
        }

        private static ConnectPointSide GetSide(this IConnectionElement connectionElement)
        {
            return LogixHelper.GetSide(connectionElement.OwnerNode, connectionElement);
        }

        private struct ConnectPoint
        {
            public readonly Slot Proxy;
            public readonly ConnectionWire Wire;

            public ConnectPoint(Slot proxy, ConnectionWire wire)
            {
                Proxy = proxy;
                Wire = wire;
            }
        }

        private class Function
        {
            public readonly float Height;
            public readonly List<Impulse> ImpulseSources = new List<Impulse>();
            public readonly List<ImpulseTargetInfo> ImpulseTargets = new List<ImpulseTargetInfo>();
            public readonly List<IInputElement> InputElements = new List<IInputElement>();
            public readonly float InputsHeight;
            public readonly List<LogixNode> LogixNodes;
            public readonly List<LogixNode> OtherConnectedNodes = new List<LogixNode>();
            public readonly List<IOutputElement> OutputElements = new List<IOutputElement>();
            public readonly float OutputsHeight;
            public readonly Slot Root;

            public Function(Slot logixFunctionRoot)
            {
                Root = logixFunctionRoot;
                LogixNodes = logixFunctionRoot.GetComponentsInChildren<LogixNode>();

                foreach (var logixNode in LogixNodes)
                {
                    var traverse = Traverse.Create(logixNode);

                    InputElements.AddRange(traverse.Property<EnumerableWrapper<IInputElement, LogixNode.InputEnumerator>>("Inputs").Value
                        .Where(input => input.TargetNode == null || input.TargetNode.FindLogixFunctionRoot() != Root));

                    OutputElements.AddRange(traverse.Property<EnumerableWrapper<IOutputElement, LogixNode.OutputEnumerator>>("Outputs").Value
                        .Where(output => !output.ConnectedInputs.Any() || output.ConnectedInputs.Any(input => input.OwnerNode.FindLogixFunctionRoot() != Root)));

                    ImpulseSources.AddRange(traverse.Property<EnumerableWrapper<Impulse, LogixNode.ImpulseEnumerator>>("ImpulseSources").Value
                        .Where(impulseSource => impulseSource.TargetNode == null || impulseSource.TargetNode.FindLogixFunctionRoot() != Root));

                    ImpulseTargets.AddRange(traverse.Property<IEnumerable<ImpulseTargetInfo>>("ImpulseTargets").Value
                        .Where(impulseTarget => !impulseTarget.Sources.Any() || impulseTarget.Sources.Any(impulseSource => impulseSource.OwnerNode.FindLogixFunctionRoot() != Root)));

                    OtherConnectedNodes.AddRange(traverse.Property<IEnumerable<LogixNode>>("OtherConnectedNodes").Value
                        .Where(node => node.FindLogixFunctionRoot() != Root));
                }

                var inputs = ImpulseTargets.Count;
                var outputs = ImpulseSources.Count;

                foreach (var input in InputElements)
                    if (input.GetSide() == ConnectPointSide.Input)
                        ++inputs;
                    else
                        ++outputs;

                foreach (var output in OutputElements)
                    if (output.GetSide() == ConnectPointSide.Input)
                        ++inputs;
                    else
                        ++outputs;

                InputsHeight = computeConnectorsHeight(inputs);
                OutputsHeight = computeConnectorsHeight(outputs);
                Height = MathX.Max(InputsHeight, OutputsHeight);
            }

            private static float computeConnectorsHeight(int count)
            {
                return count * 32f + MathX.Max(0, count - 1) * 4f;
            }
        }
    }
}