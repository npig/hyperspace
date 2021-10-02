using ImGuiNET;
using Photon.Bolt;
using UImGui;
using UnityEngine;

namespace Hyperspace
{
    public class GameMenu : UILayout
    {
        private bool _eanbleMenu = true;

        public override void Load()
        {
            UImGuiUtility.Layout += OnLayout;
            Engine.Events.Subscribe<SystemInputEvent>(OnSystemInput);
        }

        private void OnSystemInput(SystemInputEvent obj)
        {
            if (obj.KeyPressed == InputManager.MENU)
                _eanbleMenu = !_eanbleMenu;
        }

        internal override void OnLayout()
        {
            if (_eanbleMenu)
            {
                ImGui.Begin("Menu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
                ImGui.SetWindowSize(new Vector2(Screen.width / 8, Screen.height / 4));
                if (ImGui.Button("Join Game", new Vector2(ImGui.GetColumnWidth(), 18) ))
                {
                    var request = RequestSpawn.Create(GlobalTargets.OnlyServer);
                    request.CraftType = 0;
                    request.Send();
                }
                ImGui.End();
            }
        }
        
        public override void Dispose()
        {
            UImGuiUtility.Layout -= OnLayout;
            Engine.Events.Unsubscribe<SystemInputEvent>(OnSystemInput);
        }
    }
}