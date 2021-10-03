using Hyperspace.Entities;
using Hyperspace.Level;
using Hyperspace.Utils;
using Photon.Bolt;
using UdpKit;
using UnityEngine;

namespace Hyperspace.Networking
{
    [BoltGlobalBehaviour(BoltNetworkModes.Server)]
    public class ServerCallbacks : Photon.Bolt.GlobalEventListener
    {
        public static bool ListenServer = true;
        
        public override bool PersistBetweenStartupAndShutdown()
        {
            return base.PersistBetweenStartupAndShutdown();
        }

        public override void BoltStartDone()
        {
            NetworkManager.CreateSession();
        }

        public override void SceneLoadLocalDone(string scene, IProtocolToken token)
        {
            if (scene == "game")
            {
                LevelManager.Load();
            }
        }

        public override void SessionCreationFailed(UdpSession session, UdpSessionError errorReason)
        {
           Debug.Log($"{session.Id} ### {errorReason}"); 
        }

        public override void Connected(BoltConnection connection)
        {
            Player player = new Player(connection.RemoteEndPoint.Address.ToString(), connection);
            connection.UserData = player;
            connection.SetStreamBandwidth(1024 * 1024);
            NetworkManager.AddPlayer(player);
        }

        public override void OnEvent(RequestSpawn request)
        {
            BoltConnection connection = request.RaisedBy;
            CraftConfig tokenConfig = new CraftConfig(100, 1, 15);
            BoltEntity boltEntity = BoltNetwork.Instantiate(BoltPrefabs._playerShip, tokenConfig, LevelManager.GetSpawn(), Quaternion.identity);
            connection.GetPlayer().Entity = boltEntity; 
            boltEntity.AssignControl(connection);
        }
    }
}