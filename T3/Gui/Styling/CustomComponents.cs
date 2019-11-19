﻿using ImGuiNET;
using System;
using System.Numerics;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui
{
    static class CustomComponents
    {
        public static bool JogDial(string label, ref double delta, Vector2 size)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1, 0.5f));
            var hot = ImGui.Button(label + "###dummy", size);
            ImGui.PopStyleVar();
            var io = ImGui.GetIO();
            if (ImGui.IsItemActive())
            {
                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.GetForegroundDrawList().AddCircle(center, 100, Color.Gray, 50);
                hot = true;

                var pLast = io.MousePos - io.MouseDelta - center;
                var pNow = io.MousePos - center;
                var aLast = Math.Atan2(pLast.X, pLast.Y);
                var aNow = Math.Atan2(pNow.X, pNow.Y);
                delta = aLast - aNow;
                if (delta > 1.5)
                {
                    delta -= 2 * Math.PI;
                }
                else if (delta < -1.5)
                {
                    delta += 2 * Math.PI;
                }
            }

            return hot;
        }

        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        public static void SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 5;

            var backupPos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 1));

            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                offsetFromBottom =
                    (offsetFromBottom - ImGui.GetIO().MouseDelta.Y)
                   .Clamp(0, size.Y - thickness);
            }

            ImGui.SetCursorPos(backupPos);
        }

        public static bool ToggleButton(string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);
            }

            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }

            return clicked;
        }

        public static bool ToggleButton(Icon icon, string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);
            }

            if (CustomComponents.IconButton(icon, label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }

            return clicked;
        }

        public static bool IconButton(Styling.Icon icon, string label, Vector2 size)
        {
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.3f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

            var clicked = ImGui.Button((char)(int)icon + "##" + label, size);

            ImGui.PopStyleVar(2);
            ImGui.PopFont();
            return clicked;
        }


        public static void ContextMenuForItem(Action drawMenuItems)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            if (ImGui.BeginPopupContextItem("context_menu"))
            {
                drawMenuItems?.Invoke();
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar(2);
        }
        
        public static void DrawContextMenuForScrollCanvas(Action drawMenuContent, ref bool contextMenuIsOpen)
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            if (!contextMenuIsOpen && !ImGui.IsWindowFocused())
                return;
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                ImGui.GetMousePosOnOpeningCurrentPopup();
                contextMenuIsOpen = true;

                drawMenuContent.Invoke();
                ImGui.EndPopup();
            }
            else
            {
                contextMenuIsOpen = false;
            }
            ImGui.PopStyleVar(2);
        }
    }
}