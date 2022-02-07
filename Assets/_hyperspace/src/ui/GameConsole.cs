using System;
using System.Collections.Generic;
using ImGuiNET;
using Photon.Bolt;
using UImGui;
using UnityEngine;

namespace Hyperspace
{
    public class GameConsole : UILayout
    {
        private bool _eanbleMenu = false;
        private List<string> _consoleItems = new List<string>();
        
        public override void Load()
        {
            UImGuiUtility.Layout += OnLayout;
            Engine.Events.Subscribe<SystemInputEvent>(OnSystemInput);
            Engine.Events.Subscribe<ConsoleLogEvent>(OnConsoleItem);
        }

        private void OnConsoleItem(ConsoleLogEvent obj)
        {
            _consoleItems.Add(obj.Entry);
        }

        private void OnSystemInput(SystemInputEvent obj)
        {
            if (obj.KeyPressed == InputManager.CONSOLE)
                _eanbleMenu = !_eanbleMenu;
        }

        internal override void OnLayout()
        {
            if (_eanbleMenu)
            {
                ImGui.Begin("Console", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar);
                ImGui.SetWindowSize(new Vector2(Screen.width * .75f, Screen.height * .75f));
                for (int i = _consoleItems.Count - 1; i >= 0; i--)
                {
                    ImGui.Text(_consoleItems[i]);
                }
                ImGui.End();
            }
        }
        
        public override void Dispose()
        {
            UImGuiUtility.Layout -= OnLayout;
            Engine.Events.Unsubscribe<SystemInputEvent>(OnSystemInput);
            Engine.Events.Unsubscribe<ConsoleLogEvent>(OnConsoleItem);
        }
    }
}