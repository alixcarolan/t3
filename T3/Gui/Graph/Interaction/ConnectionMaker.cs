﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Handles the creation of new  <see cref="Symbol.Connection"/>s. 
    /// It provides accessors for highlighting matching input slots and methods that need to be
    /// called when connections are completed or aborted.
    /// </summary>
    public static class ConnectionMaker
    {
        public static List<TempConnection> TempConnections = new List<TempConnection>();

        public static bool IsMatchingInputType(Type valueType)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].TargetSlotId == NotConnectedId
                   //&& inputDef.DefaultValue.ValueType == _draftConnectionType;
                   && TempConnections[0].ConnectionType == valueType;
        }

        public static bool IsMatchingOutputType(Type valueType)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].SourceSlotId == NotConnectedId
                   && TempConnections[0].ConnectionType == valueType;
        }

        public static bool IsOutputSlotCurrentConnectionSource(SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].SourceParentOrChildId == sourceUi.SymbolChild.Id
                   && TempConnections[0].SourceSlotId == outputDef.Id;
        }

        public static bool IsInputSlotCurrentConnectionTarget(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            // return ConnectionSnapEndHelper.IsNextBestTarget(targetUi, inputDef.Id, 0);
            return TempConnections.Count == 1
                   && TempConnections[0].TargetParentOrChildId == targetUi.SymbolChild.Id
                   && TempConnections[0].TargetSlotId == inputDef.Id;
        }

        public static bool IsInputNodeCurrentConnectionSource(Symbol.InputDefinition inputDef)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].SourceParentOrChildId == UseSymbolContainerId
                   && TempConnections[0].SourceSlotId == inputDef.Id;
        }

        public static bool IsOutputNodeCurrentConnectionTarget(Symbol.OutputDefinition outputDef)
        {
            return TempConnections.Count == 1
                   && TempConnections[0].TargetParentOrChildId == UseSymbolContainerId
                   && TempConnections[0].TargetSlotId == outputDef.Id;
        }

        public static void StartFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition outputDef)
        {
            TempConnections.Clear();
            _isDisconnectingFromInput = false;

            var selectedSymbolChildUis = SelectionManager.GetSelectedSymbolChildUis().ToList();
            if (selectedSymbolChildUis.Count > 1 && selectedSymbolChildUis.Any(c => c.Id == sourceUi.Id))
            {
                Log.Debug("Magic would happen here?");
                foreach (var selectedChild in selectedSymbolChildUis)
                {
                    if (selectedChild.SymbolChild.Symbol.Id != sourceUi.SymbolChild.Symbol.Id)
                        return;

                    TempConnections.Add(new TempConnection(sourceParentOrChildId: selectedChild.SymbolChild.Id,
                                                           sourceSlotId: outputDef.Id,
                                                           targetParentOrChildId: NotConnectedId,
                                                           targetSlotId: NotConnectedId,
                                                           outputDef.ValueType));
                }
            }

            else
            {
                SetTempConnection(new TempConnection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                     sourceSlotId: outputDef.Id,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     outputDef.ValueType));
            }
        }

        private static bool _isDisconnectingFromInput;

        public static void StartFromInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int multiInputIndex = 0)
        {
            var existingConnection = FindConnectionToInputSlot(parentSymbol, targetUi, inputDef, multiInputIndex);
            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, multiInputIndex));
                SetTempConnection(new TempConnection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                     sourceSlotId: existingConnection.SourceSlotId,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     inputDef.DefaultValue.ValueType));
                _isDisconnectingFromInput = true;
            }
            else
            {
                SetTempConnection(new TempConnection(sourceParentOrChildId: NotConnectedId,
                                                     sourceSlotId: NotConnectedId,
                                                     targetParentOrChildId: targetUi.SymbolChild.Id,
                                                     targetSlotId: inputDef.Id,
                                                     inputDef.DefaultValue.ValueType));
                _isDisconnectingFromInput = false;
            }
        }

        public static void StartFromInputNode(Symbol.InputDefinition inputDef)
        {
            SetTempConnection(new TempConnection(sourceParentOrChildId: UseSymbolContainerId,
                                                 sourceSlotId: inputDef.Id,
                                                 targetParentOrChildId: NotConnectedId,
                                                 targetSlotId: NotConnectedId,
                                                 inputDef.DefaultValue.ValueType));
        }

        public static void StartFromOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            var existingConnection = parentSymbol.Connections.Find(c => c.TargetParentOrChildId == UseSymbolContainerId
                                                                        && c.TargetSlotId == outputDef.Id);

            if (existingConnection != null)
            {
                UndoRedoStack.AddAndExecute(new DeleteConnectionCommand(parentSymbol, existingConnection, 0));
                SetTempConnection(new TempConnection(sourceParentOrChildId: existingConnection.SourceParentOrChildId,
                                                     sourceSlotId: existingConnection.SourceSlotId,
                                                     targetParentOrChildId: NotConnectedId,
                                                     targetSlotId: NotConnectedId,
                                                     outputDef.ValueType));
            }
            else
            {
                SetTempConnection(new TempConnection(sourceParentOrChildId: NotConnectedId,
                                                     sourceSlotId: NotConnectedId,
                                                     targetParentOrChildId: UseSymbolContainerId,
                                                     targetSlotId: outputDef.Id,
                                                     outputDef.ValueType));
            }
        }

        public static void Update()
        {
            ConnectionSnapEndHelper.PrepareNewFrame();
        }

        public static void Cancel()
        {
            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        public static void CompleteAtInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input, int multiInputIndex = 0,
                                               bool insertMultiInput = false)
        {
            // TODO: Support simultaneous connection to multiInput
            var newConnection = new Symbol.Connection(sourceParentOrChildId: TempConnections[0].SourceParentOrChildId,
                                                      sourceSlotId: TempConnections[0].SourceSlotId,
                                                      targetParentOrChildId: targetUi.SymbolChild.Id,
                                                      targetSlotId: input.Id);

            bool replaceConnection = multiInputIndex % 2 != 0;
            multiInputIndex /= 2; // divide by 2 to get correct insertion index in existing connections
            var addCommand = new AddConnectionCommand(parentSymbol, newConnection, multiInputIndex);

            if (replaceConnection)
            {
                // get the previous connection
                var allConnectionsToSlot = parentSymbol.Connections.FindAll(c => c.TargetParentOrChildId == targetUi.SymbolChild.Id &&
                                                                                 c.TargetSlotId == input.Id);
                var connectionToRemove = allConnectionsToSlot[multiInputIndex];
                var deleteCommand = new DeleteConnectionCommand(parentSymbol, connectionToRemove, multiInputIndex);
                var replaceCommand = new MacroCommand("Replace Connection", new ICommand[] { deleteCommand, addCommand });
                UndoRedoStack.AddAndExecute(replaceCommand);
            }
            else
            {
                UndoRedoStack.AddAndExecute(addCommand);
            }

            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        public static void CompleteAtOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        {
            // Todo: Support simultaneous connection from multiple inputs
            var newConnection = new Symbol.Connection(sourceParentOrChildId: sourceUi.SymbolChild.Id,
                                                      sourceSlotId: output.Id,
                                                      targetParentOrChildId: TempConnections[0].TargetParentOrChildId,
                                                      targetSlotId: TempConnections[0].TargetSlotId);
            UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        #region related to SymbolBrowser
        /// <remarks>
        /// Assumes that a temp connection has be created earlier and is now dropped on the background
        /// </remarks>
        public static void InitSymbolBrowserAtPosition(SymbolBrowser symbolBrowser, Vector2 canvasPosition)
        {
            if (TempConnections.Count == 0)
                return;

            if (_isDisconnectingFromInput)
            {
                TempConnections.Clear();
                return;
            }

            var firstConnectionType = TempConnections[0].ConnectionType;
            if (TempConnections.Count == 1)
            {
                if (TempConnections[0].TargetParentOrChildId == NotConnectedId)
                {
                    SetTempConnection(new TempConnection(sourceParentOrChildId: TempConnections[0].SourceParentOrChildId,
                                                         sourceSlotId: TempConnections[0].SourceSlotId,
                                                         targetParentOrChildId: UseDraftChildId,
                                                         targetSlotId: NotConnectedId,
                                                         firstConnectionType));
                    symbolBrowser.OpenAt(canvasPosition, firstConnectionType, null, false);
                }
                else if (TempConnections[0].SourceParentOrChildId == NotConnectedId)
                {
                    SetTempConnection(new TempConnection(sourceParentOrChildId: UseDraftChildId,
                                                         sourceSlotId: NotConnectedId,
                                                         targetParentOrChildId: TempConnections[0].TargetParentOrChildId,
                                                         targetSlotId: TempConnections[0].TargetSlotId,
                                                         firstConnectionType));
                    symbolBrowser.OpenAt(canvasPosition, null, firstConnectionType, false);
                }
            }
            else if (TempConnections.Count > 1)
            {
                var validForMultiInput = TempConnections.All(c =>
                                            c.GetStatus() == TempConnection.Status.TargetIsUndefined
                                            && c.ConnectionType == firstConnectionType);
                if (validForMultiInput)
                {
                    var oldConnections = TempConnections.ToArray();
                    TempConnections.Clear();
                    foreach (var c in oldConnections) 
                    {
                        TempConnections.Add(new TempConnection(sourceParentOrChildId: c.SourceParentOrChildId ,
                                                             sourceSlotId: c.SourceSlotId,
                                                             targetParentOrChildId: UseDraftChildId,
                                                             targetSlotId: NotConnectedId,
                                                             firstConnectionType));
                    }
                    symbolBrowser.OpenAt(canvasPosition, firstConnectionType, null , onlyMultiInputs: true);                    
                }
            }
            else
            {
                Cancel();
            }
            
        }

        public static void CompleteConnectsToBuiltNode(Symbol parent, SymbolChild newSymbolChild)
        {
            foreach (var c in TempConnections)
            {
                switch (c.GetStatus())
                {
                    case TempConnection.Status.SourceIsDraftNode:
                        var outputDef = newSymbolChild.Symbol.GetOutputMatchingType(c.ConnectionType);
                        var newConnectionToSource = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                                          sourceSlotId: outputDef.Id,
                                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                                          targetSlotId: c.TargetSlotId);
                        UndoRedoStack.AddAndExecute(new AddConnectionCommand(parent, newConnectionToSource, 0));
                        break;
                    case TempConnection.Status.TargetIsDraftNode:
                        var inputDef = newSymbolChild.Symbol.GetInputMatchingType(c.ConnectionType);
                        var newConnectionToInput = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                                         sourceSlotId: c.SourceSlotId,
                                                                         targetParentOrChildId: newSymbolChild.Id,
                                                                         targetSlotId: inputDef.Id);
                        UndoRedoStack.AddAndExecute(new AddConnectionCommand(parent, newConnectionToInput, 0));
                        break;
                }
            }

            Reset();
        }
        #endregion

        public static void CompleteAtSymbolInputNode(Symbol parentSymbol, Symbol.InputDefinition inputDef)
        {
            foreach (var c in TempConnections)
            {
                var newConnection = new Symbol.Connection(sourceParentOrChildId: UseSymbolContainerId,
                                                          sourceSlotId: inputDef.Id,
                                                          targetParentOrChildId: c.TargetParentOrChildId,
                                                          targetSlotId: c.TargetSlotId);
                UndoRedoStack.AddAndExecute(new AddConnectionCommand(parentSymbol, newConnection, 0));
            }

            Reset();
        }

        public static void CompleteAtSymbolOutputNode(Symbol parentSymbol, Symbol.OutputDefinition outputDef)
        {
            foreach (var c in TempConnections)
            {
                var newConnection = new Symbol.Connection(sourceParentOrChildId: c.SourceParentOrChildId,
                                                          sourceSlotId: c.SourceSlotId,
                                                          targetParentOrChildId: UseSymbolContainerId,
                                                          targetSlotId: outputDef.Id);
                parentSymbol.AddConnection(newConnection);
            }

            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        // private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, int outputIndex)
        // {
        //     var outputId = sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].Id;
        //     return parentSymbol.Connections.FindAll(c => c.SourceSlotId == outputId
        //                                                  && c.SourceParentOrChildId == sourceUi.SymbolChild.Id);
        // }
        //
        // private static List<Symbol.Connection> FindConnectionsFromOutputSlot(Symbol parentSymbol, SymbolChildUi sourceUi, Symbol.OutputDefinition output)
        // {
        //     var outputId = output.Id;
        //     return parentSymbol.Connections.FindAll(c => c.SourceSlotId == outputId
        //                                                  && c.SourceParentOrChildId == sourceUi.SymbolChild.Id);
        // }
        //
        // private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, int inputIndex)
        // {
        //     var inputId = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].Id;
        //     return parentSymbol.Connections.Find(c => c.TargetSlotId == inputId
        //                                               && c.TargetParentOrChildId == targetUi.SymbolChild.Id);
        // }

        private static Symbol.Connection FindConnectionToInputSlot(Symbol parentSymbol, SymbolChildUi targetUi, Symbol.InputDefinition input,
                                                                   int multiInputIndex = 0)
        {
            var inputId = input.Id;
            var connections = parentSymbol.Connections.FindAll(c => c.TargetSlotId == inputId
                                                                    && c.TargetParentOrChildId == targetUi.SymbolChild.Id);
            return (connections.Count > 0) ? connections[multiInputIndex] : null;
        }

        /// <summary>
        /// A special Id the flags a connection as incomplete because either the source or the target is not yet connected.
        /// </summary>
        public static Guid NotConnectedId = Guid.Parse("eeeeeeee-E0DF-47C7-A17F-E297672EE1F3");

        /// <summary>
        /// A special Id that indicates that the source of target of a connection is not a child but an input or output node
        /// </summary>
        private static readonly Guid UseSymbolContainerId = Guid.Empty;

        /// <summary>
        /// A special id indicating that the connection is ending in the <see cref="SymbolBrowser"/>
        /// </summary>
        public static Guid UseDraftChildId = Guid.Parse("ffffffff-E0DF-47C7-A17F-E297672EE1F3");

        private static void SetTempConnection(TempConnection c)
        {
            TempConnections.Clear();
            TempConnections.Add(c);
        }

        private static void Reset()
        {
            TempConnections.Clear();
            ConnectionSnapEndHelper.ResetSnapping();
        }

        public class TempConnection : Symbol.Connection
        {
            public TempConnection(Guid sourceParentOrChildId, Guid sourceSlotId, Guid targetParentOrChildId, Guid targetSlotId, Type type) :
                base(sourceParentOrChildId, sourceSlotId, targetParentOrChildId, targetSlotId)
            {
                ConnectionType = type;
            }

            public readonly Type ConnectionType;

            public Status GetStatus()
            {
                if (TargetParentOrChildId == NotConnectedId
                    && TargetSlotId == NotConnectedId)
                {
                    return Status.TargetIsUndefined;
                }

                if (TargetParentOrChildId == UseDraftChildId
                    && TargetSlotId == NotConnectedId)
                {
                    return Status.TargetIsDraftNode;
                }

                if (SourceParentOrChildId == NotConnectedId
                    && SourceSlotId == NotConnectedId)
                {
                    return Status.SourceIsUndefined;
                }

                if (SourceParentOrChildId == UseDraftChildId
                    && SourceSlotId == NotConnectedId)
                {
                    return Status.SourceIsDraftNode;
                }

                if (SourceParentOrChildId == NotConnectedId
                    && SourceSlotId == NotConnectedId
                    && TargetParentOrChildId == NotConnectedId
                    && TargetSlotId == NotConnectedId
                    )
                {
                    return Status.NotTemporary;
                }

                Log.Warning("Found undefined connection type:" + this);
                return Status.Undefined;
            }

            public enum Status
            {
                NotTemporary,
                SourceIsUndefined,
                SourceIsDraftNode,
                TargetIsUndefined,
                TargetIsDraftNode,
                Undefined,
            }
        }

        /// <summary>
        /// A helper that collects potential collection targets during connection drag operations.
        /// </summary>
        public static class ConnectionSnapEndHelper
        {
            public static void PrepareNewFrame()
            {
                _mousePosition = ImGui.GetMousePos();
                BestMatchLastFrame = _bestMatchYetForCurrentFrame;
                if (BestMatchLastFrame != null)
                {
                    // drawList.AddRect(_bestMatchLastFrame.Area.Min, _bestMatchLastFrame.Area.Max, Color.Orange);
                    var textSize = ImGui.CalcTextSize(BestMatchLastFrame.Name);
                    ImGui.SetNextWindowPos(_mousePosition - new Vector2(textSize.X + 10, textSize.Y / 2));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                    ImGui.BeginTooltip();
                    ImGui.Text(BestMatchLastFrame.Name);
                    ImGui.EndTooltip();
                    ImGui.PopStyleVar();
                }

                _bestMatchYetForCurrentFrame = null;
                _bestMatchDistance = float.PositiveInfinity;
            }

            public static void ResetSnapping()
            {
                BestMatchLastFrame = null;
            }

            public static void RegisterAsPotentialTarget(SymbolChildUi childUi, IInputUi inputUi, int slotIndex, ImRect areaOnScreen)
            {
                if (TempConnections == null || TempConnections.Count == 0)
                    return;

                if (TempConnections.All(c => c.ConnectionType != inputUi.Type))
                    return;

                var distance = Vector2.Distance(areaOnScreen.Min, _mousePosition);
                if (distance > SnapDistance || distance > _bestMatchDistance)
                {
                    return;
                }

                _bestMatchYetForCurrentFrame = new PotentialConnectionTarget()
                                                   {
                                                       TargetParentOrChildId = childUi.SymbolChild.Id,
                                                       TargetInputId = inputUi.InputDefinition.Id,
                                                       Area = areaOnScreen,
                                                       Name = inputUi.InputDefinition.Name,
                                                       SlotIndex = slotIndex
                                                   };
                _bestMatchDistance = distance;
            }

            public static bool IsNextBestTarget(SymbolChildUi childUi, Guid inputDefinitionId, int socketIndex)
            {
                return BestMatchLastFrame != null && BestMatchLastFrame.TargetParentOrChildId == childUi.SymbolChild.Id
                                                  && BestMatchLastFrame.TargetInputId == inputDefinitionId
                                                  && BestMatchLastFrame.SlotIndex == socketIndex;
            }

            public static PotentialConnectionTarget BestMatchLastFrame;
            private static PotentialConnectionTarget _bestMatchYetForCurrentFrame;
            private static float _bestMatchDistance = float.PositiveInfinity;
            private const int SnapDistance = 50;
            private static Vector2 _mousePosition;

            public class PotentialConnectionTarget
            {
                public Guid TargetParentOrChildId;
                public Guid TargetInputId;
                public ImRect Area;
                public string Name;
                public int SlotIndex;
            }
        }
    }
}