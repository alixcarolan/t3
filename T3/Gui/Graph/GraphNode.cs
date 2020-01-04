using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.Windows;
using T3.Operators.Types;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphWindow"/>
    /// </summary>
    static class GraphNode
    {
        public static void Draw(SymbolChildUi childUi, Instance instance)
        {
            var symbolUi = SymbolUiRegistry.Entries[childUi.SymbolChild.Symbol.Id];
            var nodeHasHiddenMatchingInputs = false;
            var visibleInputUis = FindVisibleInputUis(symbolUi, childUi, ref nodeHasHiddenMatchingInputs);

            _drawList = Graph.DrawList;
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                childUi.Size = ComputeNodeSize(childUi, visibleInputUis);
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(childUi.PosOnCanvas,
                                                                               childUi.PosOnCanvas + childUi.Size));
                _lastScreenRect.Floor();

                // Resize indicator
                if (childUi.Style == SymbolChildUi.Styles.Resizable)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    ImGui.SetCursorScreenPos(_lastScreenRect.Max - new Vector2(10, 10));
                    ImGui.Button("##resize", new Vector2(10, 10));
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                    {
                        var delta = GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        childUi.Size += delta;
                    }

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                }

                // Size toggle
                {
                    var pos = new Vector2(_lastScreenRect.Max.X - 15, _lastScreenRect.Min.Y + 2);

                    ImGui.SetCursorScreenPos(pos);
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Color(0, 0, 0, .7f).Rgba);
                    if (childUi.Style == SymbolChildUi.Styles.Default)
                    {
                        if (ImGui.Button("<##size", new Vector2(16, 16)))
                        {
                            childUi.Style = SymbolChildUi.Styles.Expanded;
                        }
                    }
                    else if (childUi.Style != SymbolChildUi.Styles.Default)
                    {
                        if (ImGui.Button("v##size", new Vector2(16, 16)))
                        {
                            childUi.Style = SymbolChildUi.Styles.Default;
                        }
                    }

                    ImGui.PopStyleVar();
                    ImGui.PopStyleColor(2);
                }

                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    T3Ui.AddHoveredId(childUi.SymbolChild.Id);

                    ImGui.SetNextWindowSizeConstraints(new Vector2(200, 120), new Vector2(200, 120));
                    if (GraphCanvas.HoverMode != GraphCanvas.HoverModes.Disabled)
                    {
                        ImGui.BeginTooltip();
                        {
                            ImageCanvasForTooltips.Update();
                            if (instance.Outputs.Count > 0)
                            {
                                var firstOutput = instance.Outputs[0];
                                IOutputUi outputUi = symbolUi.OutputUis[firstOutput.Id];
                                outputUi.DrawValue(firstOutput, recompute: GraphCanvas.HoverMode == GraphCanvas.HoverModes.Live);
                            }
                        }
                        ImGui.EndTooltip();
                    }
                }

                //DrawPreview(instance);

                SelectableNodeMovement.Handle(childUi, instance);

                if (ImGui.IsItemActive() && ImGui.IsMouseDoubleClicked(0))
                {
                    GraphCanvas.Current.SetCompositionToChildInstance(instance);
                }

                if (_lastScreenRect.Contains(ImGui.GetMousePos()))
                {
                    _hoveredNodeId = childUi.Id;
                }

                var hovered = ImGui.IsItemHovered() || T3Ui.HoveredIdsLastFrame.Contains(instance.SymbolChildId);
                var drawList = GraphCanvas.Current.DrawList;

                // Rendering
                var childInstance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(c => c.SymbolChildId == childUi.SymbolChild.Id);
                var output = childInstance?.Outputs.FirstOrDefault();
                var framesSinceLastUpdate = output?.DirtyFlag.FramesSinceLastUpdate ?? 100;

                var typeColor = childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                                    ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                                    : Color.Gray;
                var backgroundColor = typeColor;
                if (framesSinceLastUpdate > 2)
                {
                    var fadeFactor = Im.Remap(framesSinceLastUpdate, 0f, 60f, 1f, 0.4f);
                    backgroundColor.Rgba.W *= fadeFactor;
                }

                // background
                drawList.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                       hovered
                                           ? ColorVariations.OperatorHover.Apply(backgroundColor)
                                           : ColorVariations.Operator.Apply(backgroundColor));

                // outline
                drawList.AddRect(_lastScreenRect.Min,
                                 _lastScreenRect.Max + Vector2.One,
                                 new Color(0.08f, 0.08f, 0.08f, 0.8f),
                                 rounding: 0,
                                 2);

                // Animation indicator
                {
                    var compositionOp = GraphCanvas.Current.CompositionOp;
                    if (compositionOp.Symbol.Animator.IsInstanceAnimated(instance))
                    {
                        _drawList.AddRectFilled(new Vector2(_lastScreenRect.Max.X - 5, _lastScreenRect.Max.Y - 12),
                                                new Vector2(_lastScreenRect.Max.X - 2, _lastScreenRect.Max.Y - 3),
                                                Color.Orange);
                    }
                }

                // Hidden inputs indicator
                if (nodeHasHiddenMatchingInputs)
                {
                    var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                    var colorForType = TypeUiRegistry.Entries[ConnectionMaker.DraftConnectionType].Color;
                    colorForType.Rgba.W *= blink;
                    _drawList.AddRectFilled(
                                            new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y + 3),
                                            new Vector2(_lastScreenRect.Min.X + 10, _lastScreenRect.Max.Y + 5),
                                            colorForType);
                }

                // Visualize update
                {
                    var updateCountThisFrame = output?.DirtyFlag.NumUpdatesWithinFrame ?? 0;
                    if (updateCountThisFrame > 0)
                    {
                        const double timeScale = 0.125f;
                        var blink = (float)(ImGui.GetTime() * timeScale * updateCountThisFrame) % 1f * _lastScreenRect.GetWidth();
                        drawList.AddRectFilled(new Vector2(_lastScreenRect.Min.X + blink, _lastScreenRect.Min.Y),
                                               new Vector2(_lastScreenRect.Min.X + blink + 2, _lastScreenRect.Max.Y),
                                               new Color(0.06f));
                    }
                }

                // Label
                drawList.PushClipRect(_lastScreenRect.Min, _lastScreenRect.Max, true);
                ImGui.PushFont(GraphCanvas.Current.Scale.X < 1 ? Fonts.FontSmall : Fonts.FontBold);
                var isRenamed = !string.IsNullOrEmpty(childUi.SymbolChild.Name);

                drawList.AddText(_lastScreenRect.Min + LabelPos,
                                 ColorVariations.OperatorLabel.Apply(typeColor),
                                 string.Format(isRenamed ? ("\"" + childUi.SymbolChild.ReadableName + "\"") : childUi.SymbolChild.ReadableName));
                ImGui.PopFont();
                drawList.PopClipRect();

                if (childUi.IsSelected)
                {
                    drawList.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }
            }
            ImGui.PopID();

            // Input Sockets...
            for (var inputIndex = 0; inputIndex < visibleInputUis.Count; inputIndex++)
            {
                var inputUi = visibleInputUis[inputIndex];
                var inputDefinition = inputUi.InputDefinition;

                var usableSlotArea = GetUsableInputSlotSize(inputIndex, visibleInputUis.Count);

                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + inputDefinition.GetHashCode());
                ImGui.SetCursorScreenPos(usableSlotArea.Min);
                ImGui.InvisibleButton("input", usableSlotArea.GetSize());
                THelpers.DebugItemRect("input-slot");

                // Note: isItemHovered does not work when being dragged from another item
                var hovered = ConnectionMaker.TempConnection != null
                                  ? usableSlotArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered();

                var isPotentialConnectionTarget = ConnectionMaker.IsMatchingInputType(inputDefinition.DefaultValue.ValueType);
                var colorForType = ColorForInputType(inputDefinition);

                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, inputDefinition.Id);

                // Render input Label
                {
                    var inputLabelOpacity = Im.Remap(GraphCanvas.Current.Scale.X,
                                                     0.75f, 1.5f,
                                                     0f, 1f);

                    var screenCursor = usableSlotArea.GetCenter() + new Vector2(14, -7);
                    if (inputLabelOpacity > 0)
                    {
                        ImGui.PushFont(Fonts.FontSmall);
                        var labelColor = ColorVariations.OperatorLabel.Apply(colorForType);
                        labelColor.Rgba.W = inputLabelOpacity;
                        var label = inputDefinition.Name;
                        if (inputDefinition.IsMultiInput)
                        {
                            label += " [...]";
                        }

                        var labelSize = ImGui.CalcTextSize(label);
                        _drawList.AddText(screenCursor, labelColor, label);

                        screenCursor += new Vector2(labelSize.X + 8, 0);

                        // Value
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor.Rgba);
                        var inputSlot = instance.Inputs.Single(slot => inputDefinition.Id == slot.Id);
                        var valueAsString = inputUi.GetSlotValue(inputSlot);

                        var valueColor = labelColor;
                        valueColor.Rgba.W *= 0.6f;
                        _drawList.AddText(screenCursor, valueColor, valueAsString);
                        ImGui.PopStyleColor();

                        ImGui.PopFont();
                    }
                }

                if (inputDefinition.IsMultiInput)
                {
                    var showGaps = isPotentialConnectionTarget;

                    var socketCount = showGaps
                                          ? connectedLines.Count * 2 + 1
                                          : connectedLines.Count;

                    var socketHeight = (usableSlotArea.GetHeight() + 1) / socketCount;
                    var targetPos = new Vector2(usableSlotArea.Max.X - 2,
                                                usableSlotArea.Min.Y + socketHeight * 0.5f);

                    var topLeft = new Vector2(usableSlotArea.Min.X, usableSlotArea.Min.Y);
                    var socketSize = new Vector2(usableSlotArea.GetWidth(), socketHeight - SlotGaps);

                    var reactiveSlotColor = GetReactiveSlotColor(inputDefinition.DefaultValue.ValueType, colorForType, SocketDirections.Input);

                    for (var index = 0; index < socketCount; index++)
                    {
                        var usableSocketArea = new ImRect(topLeft, topLeft + socketSize);
                        var isSocketHovered = usableSocketArea.Contains(ImGui.GetMousePos());

                        bool isGap = false;
                        if (showGaps)
                        {
                            isGap = (index & 1) == 0;
                        }

                        if (!isGap)
                        {
                            var line = showGaps
                                           ? connectedLines[index >> 1]
                                           : connectedLines[index];

                            line.TargetPosition = targetPos;
                            line.IsSelected |= childUi.IsSelected;
                        }

                        DrawMultiInputSocket(childUi, inputDefinition, usableSocketArea, isSocketHovered, index, isGap, colorForType, reactiveSlotColor);

                        targetPos.Y += socketHeight;
                        topLeft.Y += socketHeight;
                    }

                    _drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - 8, usableSlotArea.Min.Y),
                                            new Vector2(usableSlotArea.Max.X - 1, usableSlotArea.Min.Y + 2),
                                            reactiveSlotColor);

                    _drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - 8, usableSlotArea.Max.Y - 2),
                                            new Vector2(usableSlotArea.Max.X - 1, usableSlotArea.Max.Y),
                                            reactiveSlotColor);
                }
                else
                {
                    foreach (var line in connectedLines)
                    {
                        line.TargetPosition = new Vector2(usableSlotArea.Max.X - 1,
                                                          usableSlotArea.GetCenter().Y);
                        line.IsSelected |= childUi.IsSelected;
                    }

                    DrawInputSlot(childUi, inputDefinition, usableSlotArea, colorForType, hovered);
                }

                ImGui.PopID();
            }

            // Outputs sockets...
            var outputIndex = 0;
            foreach (var output in childUi.SymbolChild.Symbol.OutputDefinitions)
            {
                var usableArea = GetUsableOutputSlotArea(childUi, outputIndex);
                ImGui.SetCursorScreenPos(usableArea.Min);
                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + output.Id.GetHashCode());

                ImGui.InvisibleButton("output", usableArea.GetSize());
                THelpers.DebugItemRect();
                var valueType = output.ValueType;
                var colorForType = TypeUiRegistry.Entries[valueType].Color;

                //Note: isItemHovered does not work when dragging is active
                var hovered = ConnectionMaker.TempConnection != null
                                  ? usableArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered();

                foreach (var line in Graph.Connections.GetLinesFromNodeOutput(childUi, output.Id))
                {
                    line.SourcePosition = new Vector2(usableArea.Max.X, usableArea.GetCenter().Y);
                    line.ColorForType = colorForType;
                    line.IsSelected |= childUi.IsSelected;
                }

                DrawOutput(childUi, output, usableArea, colorForType, hovered);

                outputIndex++;
            }
        }

        static readonly List<IInputUi> VisibleInputs = new List<IInputUi>(15);

        // Find visible input slots.
        // TODO: this is a major performance hot spot and needs optimization
        static List<IInputUi> FindVisibleInputUis(SymbolUi symbolUi, SymbolChildUi childUi, ref bool nodeHasHiddenMatchingInputs)
        {
            var connectionsToNode = Graph.Connections.GetLinesIntoNode(childUi);

            if (childUi.Style == SymbolChildUi.Styles.Expanded)
            {
                return (from inputUi in symbolUi.InputUis.Values
                        orderby inputUi.Index
                        select inputUi).ToList();
            }

            var isNodeHoveredConnectionTarget = _hoveredNodeId == childUi.Id
                                                && ConnectionMaker.TempConnection != null
                                                && ConnectionMaker.TempConnection.TargetParentOrChildId == ConnectionMaker.NotConnectedId;

            VisibleInputs.Clear();
            foreach (var inputUi in symbolUi.InputUis.Values)
            {
                bool inputIsConnectionTarget = false;
                for (int i = 0; i < connectionsToNode.Count; i++)
                {
                    if (connectionsToNode[i].Connection.TargetSlotId == inputUi.Id)
                    {
                        inputIsConnectionTarget = true;
                        break;
                    }
                }

                if (inputUi.Relevancy != Relevancy.Optional || inputIsConnectionTarget)
                {
                    VisibleInputs.Add(inputUi);
                }
                else if (ConnectionMaker.IsMatchingInputType(inputUi.Type))
                {
                    if (isNodeHoveredConnectionTarget)
                    {
                        VisibleInputs.Add(inputUi);
                    }
                    else
                    {
                        nodeHasHiddenMatchingInputs = true;
                    }
                }
            }

            return VisibleInputs.OrderBy(ui => ui.Index).ToList();
        }

        private enum SocketDirections
        {
            Input,
            Output,
        }

        private static Color GetReactiveSlotColor(Type type, Color colorForType, SocketDirections direction)
        {
            var style = direction == SocketDirections.Input
                            ? ColorVariations.ConnectionLines
                            : ColorVariations.Operator;
            if (ConnectionMaker.TempConnection != null)
            {
                if (direction == SocketDirections.Input
                        ? ConnectionMaker.IsMatchingInputType(type)
                        : ConnectionMaker.IsMatchingOutputType(type))
                {
                    var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                    colorForType.Rgba.W *= blink;
                    style = ColorVariations.Highlight;
                }
                else
                {
                    style = ColorVariations.Muted;
                }
            }

            return style.Apply(colorForType);
        }

        
        private static void DrawPreview(Instance instance)
        {
            if (instance.Outputs.Count == 0)
                return;

            var firstOutput = instance.Outputs[0];
            if (!(firstOutput is Slot<Texture2D> textureSlot))
                return;

            var texture = textureSlot.Value;
            if (texture == null)
                return;

            ShaderResourceView srv;
            try
            {
                srv = new ShaderResourceView(ResourceManager.Instance().Device, texture);
            }
            catch (Exception e)
            {
                Log.Warning("ImageOutputCanvas::DrawTexture(...) - Could not create ShaderResourceView for texture.");
                Log.Warning(e.Message);
                return;
            }

            var aspect = (float)texture.Description.Width / texture.Description.Height; 
            var previewHeight = _lastScreenRect.GetWidth() / aspect;

            Graph.DrawList.AddImage((IntPtr)srv,
                        new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Min.Y - previewHeight-1),
                        new Vector2(_lastScreenRect.Max.X, _lastScreenRect.Min.Y-1));
        }

        
        private static Vector2 ComputeNodeSize(SymbolChildUi childUi, List<IInputUi> visibleInputUis)
        {
            if (childUi.Style == SymbolChildUi.Styles.Resizable)
            {
                return childUi.Size;
            }

            var additionalMultiInputSlots = 0;
            foreach (var input in visibleInputUis)
            {
                if (!input.InputDefinition.IsMultiInput)
                    continue;

                //TODO: this should be refactored, because it's very slow and is later repeated
                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, input.Id);
                additionalMultiInputSlots += connectedLines.Count;
            }

            return new Vector2(SymbolChildUi.DefaultOpSize.X,
                               23 + (visibleInputUis.Count + additionalMultiInputSlots) * 13);
        }

        private static void DrawOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (ConnectionMaker.IsOutputSlotCurrentConnectionSource(childUi, outputDef))
            {
                _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                        ColorVariations.Highlight.Apply(colorForType));

                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingOutputType(outputDef.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name}<{TypeNameRegistry.Entries[outputDef.ValueType]}>");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
            }
            else
            {
                var color = GetReactiveSlotColor(outputDef.ValueType, colorForType, SocketDirections.Output);
                var pos = usableArea.Min;
                _drawList.AddRectFilled(
                                        pos,
                                        usableArea.Max,
                                        color
                                       );
            }
        }

        private static ImRect GetUsableOutputSlotArea(SymbolChildUi targetUi, int outputIndex)
        {
            var thickness = Im.Remap(GraphCanvas.Current.Scale.X, 0.5f, 1f, 3f, UsableSlotThickness);

            var opRect = _lastScreenRect;
            var outputCount = targetUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputHeight = outputCount == 0
                                   ? opRect.GetHeight()
                                   : (opRect.GetHeight() - 1 + SlotGaps) / outputCount - SlotGaps;
            if (outputHeight <= 0)
                outputHeight = 1;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   opRect.Max.X + 1, // - GraphNode._usableSlotThickness,
                                                   opRect.Min.Y + (outputHeight + SlotGaps) * outputIndex + 1
                                                  ),
                                       new Vector2(
                                                   thickness,
                                                   outputHeight
                                                  ));
        }

        private static void DrawInputSlot(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                                            usableArea.Min,
                                            usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType)
                                           );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}<{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
            }
            else
            {
                var connectionColor = GetReactiveSlotColor(inputDef.DefaultValue.ValueType, colorForType, SocketDirections.Input);
                var pos = new Vector2(
                                      usableArea.Max.X - GraphNode.InputSlotThickness - InputSlotMargin,
                                      usableArea.Min.Y
                                     );
                var size = new Vector2(GraphNode.InputSlotThickness, usableArea.GetHeight());
                _drawList.AddRectFilled(
                                        pos,
                                        pos + size,
                                        connectionColor
                                       );
            }
        }

        private static void DrawMultiInputSocket(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea,
                                                 bool isInputHovered, int multiInputIndex, bool isGap, Color colorForType,
                                                 Color reactiveSlotColor)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef, multiInputIndex))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (isInputHovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                                            usableArea.Min,
                                            usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType)
                                           );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}[{multiInputIndex}] <{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                        Log.Debug("started connection at MultiInputIndex:" + multiInputIndex);
                    }
                }
            }
            else
            {
                //var pos = usableArea.Min + Vector2.UnitY * GraphNode._inputSlotMargin;
                var gapOffset = isGap ? new Vector2(2, 0) : Vector2.Zero;
                var pos = new Vector2(usableArea.Max.X - InputSlotMargin - InputSlotThickness,
                                      usableArea.Min.Y) - gapOffset;
                var size = new Vector2(InputSlotThickness, usableArea.GetHeight()) + gapOffset;
                _drawList.AddRectFilled(
                                        pos,
                                        pos + size,
                                        reactiveSlotColor
                                       );
            }
        }

        private static float _nodeTitleHeight = 22;

        private static ImRect GetUsableInputSlotSize(int inputIndex, int visibleSlotCount)
        {
            var areaForParams = new ImRect(new Vector2(
                                                       _lastScreenRect.Min.X,
                                                       _lastScreenRect.Min.Y + _nodeTitleHeight),
                                           _lastScreenRect.Max);
            var inputHeight = visibleSlotCount == 0
                                  ? areaForParams.GetHeight()
                                  : (areaForParams.GetHeight() + SlotGaps) / visibleSlotCount - SlotGaps;
            if (inputHeight <= 0)
                inputHeight = 1;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   areaForParams.Min.X - UsableSlotThickness,
                                                   areaForParams.Min.Y + (inputHeight + SlotGaps) * inputIndex
                                                  ),
                                       new Vector2(
                                                   UsableSlotThickness,
                                                   inputHeight
                                                  ));
        }

        private static Color ColorForInputType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        #region style variables
        public static Vector2 LabelPos = new Vector2(4, 4);
        public static float UsableSlotThickness = 10;
        public static float InputSlotThickness = 3;
        public static float InputSlotMargin = 1;
        public static float SlotGaps = 2;
        public static float OutputSlotMargin = 1;
        #endregion

        private static readonly ImageOutputCanvas ImageCanvasForTooltips = new ImageOutputCanvas();
        private static Guid _hoveredNodeId;

        private static ImRect _lastScreenRect;
        private static ImDrawListPtr _drawList;
    }
}