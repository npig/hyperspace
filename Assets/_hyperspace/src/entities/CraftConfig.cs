using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Photon.Bolt;
using Photon.Bolt.Utils;
using UdpKit;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hyperspace.Entities
{
    public enum CraftClass
    {
        LIGHT,
        MEDIUM,
        HEAVY
    }
    
    public class CraftConfig : IProtocolToken
    {
        public float Speed { get; private set; }
        public int Energy { get; private set; }
        public int Armor; 
        private CraftClass _craftClass;

        public CraftConfig(int energy, int armor, float speed)
        {
            Speed = speed;
            Energy = energy;
            Armor = armor;
        }

        public CraftConfig()
        {
            
        }

        public void Read(UdpPacket packet)
        {
            Speed = packet.ReadFloat();
            Energy = packet.ReadInt();
        }

        public void Write(UdpPacket packet)
        {
            packet.WriteFloat(Speed);
            packet.WriteInt(Energy); 
        }
    }

    public class ProjectileData : IProtocolToken
    {
        public float Speed { get; set; }
        public Vector3 Velocity  { get; set; }
        public Vector3 Direction  { get; set; }
        
        public ProjectileData(){}

        public ProjectileData(Vector3 velocity, Vector3 direction, float speed)
        {
            Velocity = velocity;
            Direction = direction;
            Speed = speed;
        }

        public void Read(UdpPacket packet)
        {
            Speed = packet.ReadFloat();
            Velocity = packet.ReadVector3();
            Direction = packet.ReadVector3();
        }

        public void Write(UdpPacket packet)
        {
            packet.WriteFloat(Speed);
            packet.WriteVector3(Velocity); 
            packet.WriteVector3(Direction); 
        }
    }

    public class ProjectileBase
    {
        private const string SERVER_PROJECTILE = "projectileServer";
        private const string CLIENT_PROJECTILE = "projectileClient";
        
        private GameObject ServerProjectile;
        private GameObject ClientProjectile;
        
        public int fireFrame;

        public ProjectileBase()
        {
            _ = LoadPrefabs();
        }

        public async UniTaskVoid LoadPrefabs()
        {
            ServerProjectile = await Engine.LoadAsset<GameObject>(SERVER_PROJECTILE);
            ClientProjectile = await Engine.LoadAsset<GameObject>(CLIENT_PROJECTILE);
        }

        public virtual void OnOwner (CraftCommand cmd, BoltEntity entity)
		{
            GameObject proj = GameObject.Instantiate(ServerProjectile, entity.transform.position, Quaternion.LookRotation(entity.transform.forward));
        }

		public virtual void OnClient (BoltEntity entity)
		{
            GameObject proj = GameObject.Instantiate(ClientProjectile, entity.transform.position, Quaternion.LookRotation(entity.transform.forward));
		}
    }

    public class Player : IDisposable
    {
        public string Name { get; set; }
        public BoltEntity Entity { get; set; }
        public BoltConnection Connection { get; set; }

        public bool IsReady;
        public static Player serverPlayer;

        public IPlayerShipState State => Entity.GetState<IPlayerShipState>();
        
        public Player(string name, BoltConnection connection)
        {
            Name = name;
            Connection = connection;
        }
        
        public void Dispose()
        {
            
        }
    }
    
    
    public class EntityManager : EngineService
    {
        public static void SpawnCraftAtPosition(BoltConnection connection)
        {
            /*if(spawnedEntities.ContainsKey(connection))
                return;
            
           // connection.UserData = new 
            
            CraftData tokenData = new CraftData(100, 1, 15, CraftClass.LIGHT);
            BoltEntity boltEntity = BoltNetwork.Instantiate(BoltPrefabs._playerShip, tokenData, Vector3.zero, Quaternion.identity);
            spawnedEntities.Add(connection, boltEntity);*/
        }
    }
}