using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using UdpKit;
using UnityEngine;

namespace Hyperspace
{
    public class GameMenu : UILayout
    {
        private int _energy = 100;
        
        internal override void OnLayout()
        {
            ImGui.Begin("HYPERSPACE", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);
            ImGui.SetWindowPos(new Vector2( 10, 10));
            ImGui.SetWindowSize(new Vector2(Screen.width / 10, Screen.height / 2));
            
            if (ImGui.Button("Join"))
            {
                var request = RequestSpawn.Create(GlobalTargets.OnlyServer);
                request.CraftType = 0;
                request.Send();
            }
            
            /*
            ImGui.BeginPopupContextWindow("nrg_stat");
            ImGui.Text($"Player");
            ImGui.SameLine();
            ImGui.SliderInt("Energy", ref _energy, 0, 100);
            ImGui.EndPopup();
            
            ImGui.OpenPopup("nrg_stat");*/
            
            ImGui.End();
        }
    }
    
    public class MainMenu : UILayout
    {
        private int _selectedItem = 0;
        private string[] _serverList = new string[] { "no servers found" }; 
        
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