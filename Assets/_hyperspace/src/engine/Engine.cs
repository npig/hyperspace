using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hyperspace.Entities;
using Hyperspace.Networking;
using Hyperspace.Utils;
using ImGuiNET;
using Photon.Bolt;
using Photon.Bolt.Matchmaking;
using UdpKit;
using UImGui;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.PlayerLoop;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using WebSocketSharp;

namespace Hyperspace
{
    public static class Engine
    {
        public static EventBus Events { get; private set; }
        public static Camera Camera { get; private set; }
        public static DearImGui DearImGui { get; private set; }
        public static NetworkManager NetworkManager { get; private set; }
        public static EntityManager EntityManager { get; private set; }
        public static UIManager UIManager { get; private set; }
        public static InputManager InputManager { get; private set; }

        private static void StartServer() => BoltLauncher.StartServer();
        private static void StartClient() => BoltLauncher.StartClient();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialise()
        {
            Debug.Log("LOADING");
            
            //GlobalManager
            ServiceManager.Initialise();
            //EventSystem
            Events = ServiceManager.CreateService<EventBus>();
            //Camera
            Camera = ServiceManager.CreateService<Camera>();
            //Networking
            //Input
            //ObjectPool
            //PlayerShip
            //Analytics
            //Dear ImGui: Bloat-free Graphical User interface
            DearImGui = ServiceManager.CreateService<DearImGui, UnityEngine.Camera>(Camera.MainCamera);
            NetworkManager = ServiceManager.CreateService<NetworkManager>();
            EntityManager = ServiceManager.CreateService<EntityManager>();
            InputManager = ServiceManager.CreateService<InputManager>();
            UIManager = ServiceManager.CreateService<UIManager>();

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("EnableUIDev"))
            {
                UIManager.LoadScreen(new GameMenu());
                return;
            }
            
            if (EditorPrefs.GetBool("EnableServer"))
            {
                StartServer();
                Application.targetFrameRate = 60;
                return;
            }
#endif
            
            if (CheckServerMode())
            {
                StartServer();
                Application.targetFrameRate = 60;
            }
            else
            {
                StartClient();
            }
        }

        public static bool IsHeadlessMode()
        {
            return Environment.CommandLine.Contains("-batchmode") && Environment.CommandLine.Contains("-nographics");
        }
        
        public static bool CheckServerMode()
        {
            return Environment.CommandLine.Contains("-server");
        }

        static string GetArguments(params string[] names)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                foreach (var name in names)
                {
                    if (args[i] == name && args.Length > i + 1)
                    {
                        return args[i + 1];
                    }
                }
            }

            return null;
        }
        
        public static async UniTask<T> LoadAsset<T>(string asset)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(asset);
            return await handle.ToUniTask();
        }
    }

    public class Camera : EngineService
    {
        public UnityEngine.Camera MainCamera { get; private set; }

        public Camera()
        {
            MainCamera = new GameObject("Service: Main Camera", typeof(UnityEngine.Camera))
                .GetComponent<UnityEngine.Camera>();
            MonoBehaviour.DontDestroyOnLoad(MainCamera);
            MainCamera.transform.SetPositionAndRotation(
                new Vector3(0, 10, 0),
                Quaternion.Euler(new Vector3(90, 0, 0))
            );
            MainCamera.orthographic = true;
            MainCamera.orthographicSize = 12;
            MainCamera.clearFlags = CameraClearFlags.SolidColor;
            MainCamera.backgroundColor = new Color(.1f,.1f,.1f);
        }
    }

    public class NetworkManager : EngineService
    {
        //Server
        private static Map<Guid, UdpSession> _sessionList = new Map<Guid, UdpSession>();
        private static List<Player> _playerList = new List<Player>();
        
        //Client
        private static BoltConnection _localConnection;
        public static Player LocalPlayer => _localConnection?.GetPlayer();
        
        public static void SetLocalConnection(BoltConnection connection)
        {
            _localConnection = connection;
        }
        
        //Server
        public static void AddPlayer(Player player)
        {
            _playerList.Add(player);
        }

        public static IEnumerable<Player> GetPlayers()
        {
            return _playerList;
        }
        
        //Client
        public static void SetSessionList(Map<Guid, UdpSession> sessionList)
        {
            _sessionList = sessionList;
        }
        
        public static Map<Guid, UdpSession> GetSessionList()
        {
            return _sessionList;
        }

        public static void CreateSession()
        {
            if(!BoltNetwork.IsServer) 
               return;
            
            string matchName = Guid.NewGuid().ToString();

            BoltMatchmaking.CreateSession(
                sessionID: matchName,
                sceneToLoad: "game"
            );
        }
    }

    public class InputState 
    {
        public Vector3? Controller { get; set; }
        public bool Thrust { get; set; }
        public bool LightFire { get; set; }
        public bool HeavyFire { get; set; }
        public bool AbilityA { get; set; }
        public bool AbilityB { get; set; }
    }

    public class CraftState
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
    }

    public class SystemInputEvent : Event
    {
        public KeyCode KeyPressed { get; set; }

        public SystemInputEvent Init(KeyCode keyPressed)
        {
            KeyPressed = keyPressed;
            return this;
        }
    }
    
    public class InputManager : EngineService
    {
        public static KeyCode THRUST = KeyCode.W;
        public static int LGHT_FIRE = 0;
        public static int HVY_FIRE = 1;
        public static KeyCode ABILITY_A = KeyCode.Q;
        public static KeyCode ABILITY_B = KeyCode.E;
        public static KeyCode MENU = KeyCode.Tab;

        private InputState _inputState = new InputState();

        public override void OnTick()
        {
            CraftInput();
            SystemInput();
        }

        public ICraftCommandInput GetInputState(ICraftCommandInput craftCommandInput)
        {
            craftCommandInput.Controller = _inputState.Controller ?? Vector3.zero;
            craftCommandInput.Thrust = _inputState.Thrust;
            craftCommandInput.LightFire = _inputState.LightFire;
            craftCommandInput.HeavyFire = _inputState.HeavyFire;
            craftCommandInput.AbilityA = _inputState.AbilityA;
            craftCommandInput.AbilityB = _inputState.AbilityB;
            return craftCommandInput;
        }

        private void CraftInput()
        {
            _inputState.Controller = MousePosition();
            _inputState.Thrust = Input.GetKey(THRUST);
            _inputState.LightFire = Input.GetMouseButton(LGHT_FIRE);
            _inputState.HeavyFire = Input.GetMouseButton(HVY_FIRE);
            _inputState.AbilityA = Input.GetKeyDown(ABILITY_A);
            _inputState.AbilityB = Input.GetKeyDown(ABILITY_B);
        }

        private void SystemInput()
        {
            if (Input.GetKeyDown(MENU))
                Engine.Events.Emit<SystemInputEvent>(ObjectPool.Get<SystemInputEvent>().Init(MENU));
        }
        
        private Vector3? MousePosition()
        {
            Vector3? mousePosition = null;
            int layer = (1 << 8);
            Ray ray = Hyperspace.Engine.Camera.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layer))
            {
                mousePosition = hit.point;
            }

            return mousePosition;
        }
    }

    public abstract class UILayout : IDisposable
    {
        public virtual void Load()
        {
            UImGuiUtility.Layout += OnLayout;
        }

        internal abstract void OnLayout();

        public virtual void Dispose()
        {
            UImGuiUtility.Layout -= OnLayout;
        }
    }

    public class UIManager : EngineService
    {
        private UILayout _currentLayout;

        public void LoadScreen(UILayout layout)
        {
            _currentLayout?.Dispose();
            _currentLayout = layout;
            _currentLayout.Load();
        }

        public void Clear()
        {
            _currentLayout?.Dispose();
            _currentLayout = null;
        }
        
        public override void OnShutdown()
        {
            _currentLayout?.Dispose();
            _currentLayout = null;
        }
    }
}