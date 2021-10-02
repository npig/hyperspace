using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using UdpKit;
using UImGui;
using UnityEngine;

namespace Hyperspace
{
    public class GameMenu : UILayout
    {
        private int _energy = 100;
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
            ImGui.Begin("Header", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs);
            ImGui.SetWindowPos(new Vector2(0, 0));
            ImGui.SetWindowSize(new Vector2(Screen.width, Screen.height / 16));
            float posX = (ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcItemWidth() + ImGui.GetStyle().ItemSpacing.x) / 2; 
            ImGui.SetCursorPosX(posX);
            bool v = ImGui.SliderInt("", ref _energy, 0, 10);
            ImGui.End();

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
    
    public class MainMenu : UILayout
    {
        private int _selectedItem = 0;
        private string[] _serverList = new string[] { "no servers found" };
        private int _nrg = 100;
        
        internal override void OnLayout()
        {
            ImGui.Begin("HYPERSPACE");
            ImGui.SetWindowSize(new Vector2(Screen.width / 4, Screen.height / 4));
            ImGui.SetWindowPos(new Vector2(Screen.width / 2 - Screen.width / 8, Screen.height / 2));
            
            ImGui.Text($"Server List");
            
            ImGui.ListBox("", ref _selectedItem, GetServerList(), _serverList.Length, 4);
            if (ImGui.Button("Join"))
            {
                Debug.Log("Joining");
                KeyValuePair<Guid, UdpSession> session = NetworkManager.GetSessionList().ElementAt(_selectedItem);
                UdpSession photonSession = session.Value as UdpSession;
                BoltMatchmaking.JoinSession(photonSession);
                Engine.UIManager.Clear();
            }
            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
            {
                Debug.Log("Refresh");
                _serverList = BuildServerList();
            }
            ImGui.SameLine();
            if (ImGui.Button("Quit"))
            {
                Debug.Log("Quit");
            }
            ImGui.End();
        }

        private string[] GetServerList()
        {
            return _serverList;
        }

        private string[] BuildServerList()
        {
            var sessionList = NetworkManager.GetSessionList(); 
            
            if (sessionList.Count == 0)
               return new [] { "no servers found" }; 
            
            string[] sessions = new string[sessionList.Count];

            for (int i = 0; i < sessionList.Count; i++)
            {
                var session = sessionList.ElementAt(i);
                sessions[i] = $"{session.Value.HostName} - {session.Value.ConnectionsCurrent} / {session.Value.ConnectionsMax}";
            }

            return sessions;
        }
    }
}