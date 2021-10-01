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
                //Engine.UIManager.LoadWindow(new GUI());
            }
        }
        
        public override void BoltStartDone()
        {
            Engine.UIManager.LoadScreen(new MainMenu());
        }

        public override void Connected(BoltConnection connection) { }

        public override void ControlOfEntityGained(BoltEntity entity)
        {
            base.ControlOfEntityGained(entity);
        }

        public override void SessionConnected(UdpSession session, IProtocolToken token)
        {
            
        }
        
        public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
        {
            Debug.LogFormat("Session list updated: {0} total sessions", sessionList.Count);
            NetworkManager.SetSessionList(sessionList);
        }
    }
}