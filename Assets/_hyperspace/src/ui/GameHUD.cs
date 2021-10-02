using ImGuiNET;
using Photon.Bolt;
using UImGui;
using UnityEngine;

namespace Hyperspace
{
    public class GameHUD : UILayout
    {
        private IPlayerShipState _playerShipState;

        public GameHUD(BoltEntity entity)
        {
            _playerShipState = entity.GetState<IPlayerShipState>();
        }

        public override void Load()
        {
            UImGuiUtility.Layout += OnLayout;
        }

        internal override void OnLayout()
        {
            int energy = _playerShipState.CraftData.Energy; 
            ImGui.Begin("Header", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs);
            ImGui.SetWindowPos(new Vector2(0, 0));
            ImGui.SetWindowSize(new Vector2(Screen.width, Screen.height / 16));
            float posX = (ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcItemWidth() + ImGui.GetStyle().ItemSpacing.x) / 2; 
            ImGui.SetCursorPosX(posX);
            bool v = ImGui.SliderInt("", ref energy, 0, 100);
            ImGui.End();
        }
    }
}