﻿using ImGuiNET;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows.Output
{
    public class EditResolutionDialog : ModalDialog
    {
        public void Draw(ResolutionHandling.Resolution resolution)
        {
            if (BeginDialog("Edit output resolution"))
            {
                ImGui.SetNextItemWidth(120);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Name:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##parameterName", ref resolution.Title, 255);

                ImGui.Spacing();

                ImGui.SetNextItemWidth(120);
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted("Resolution:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250);

                var res = new int[2] { resolution.Size.Width, resolution.Size.Height };
                ImGui.DragInt2("##resolution", ref res[0], 255);
                resolution.Size.Width = res[0];
                resolution.Size.Height = res[1];
                
                ImGui.Checkbox("Use as aspect ratio", ref resolution.UseAsAspectRatio);

                if (CustomComponents.DisablableButton("Add", resolution.IsValid))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    if (!resolution.IsValid)
                    {
                        ResolutionHandling.Resolutions.Remove(resolution);
                    }
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }
    }
}