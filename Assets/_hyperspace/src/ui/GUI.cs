using ImGuiNET;
using UnityEngine;

namespace Hyperspace
{
    public class GUI : UILayout
    {
        private int _energy = 0;
        
        internal override void OnLayout()
        {
            ImGui.Begin(
                "HYPERSPACE",
                ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoTitleBar);
            ImGui.SetWindowSize(new Vector2(Screen.width / 3, Screen.height / 8));
            ImGui.SetWindowPos(new Vector2( 0, 50));
            ImGui.Text($"Player");
            ImGui.SameLine();
            ImGui.SliderInt("Energy", ref _energy, 0, 100);
            ImGui.End();
        }
    }
}