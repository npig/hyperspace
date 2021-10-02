using System;
using Photon.Bolt;
using UdpKit;
using UnityEngine;

namespace Hyperspace.Networking
{
    [BoltGlobalBehaviour(BoltNetworkModes.Client)]
    public class ClientCallbacks : Photon.Bolt.GlobalEventListener
    {
        public override void SceneLoadLocalDone(string scene, IProtocolToken token)
        {
            if (scene == "game")
            {
                Engine.UIManager.LoadScreen(new GameMenu());
            }
        }
        
        public override void BoltStartDone()
        {
            Engine.UIManager.LoadScreen(new MainScreen());
        }

        public override void Connected(BoltConnection connection)
        {
            NetworkManager.SetLocalConnection(connection);
        }

        public override void SessionConnected(UdpSession session, IProtocolToken token)
        {
            
        }
        
        public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
        {
            Debug.LogFormat("Session list updated: {0} total sessions", sessionList.Count);
            NetworkManager.SetSessionList(sessionList);
        }

        public override void ControlOfEntityGained(BoltEntity entity)
        {
            UILayout layout = new GameHUD(entity);
            layout.Load();
        }
    }
}