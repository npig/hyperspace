using System.Linq;
using ImGuiNET;
using UImGui;
using UImGui.Assets;
using UImGui.Platform;
using UImGui.Renderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hyperspace
{
    // This component is responsible for setting up ImGui for use in Unity.
    // It holds the necessary context and sets it up before any operation is done to ImGui.
    // (e.g. set the context, texture and font managers before calling Layout)

    /// <summary>
    /// Dear ImGui integration into Unity
    /// </summary>
    public class DearImGui : EngineService 
    {
        Context _context;
        IRenderer _renderer;
        IPlatform _platform;
        CommandBuffer _cmd;

        public event System.Action Layout;  // Layout event for *this* ImGui instance
        bool _doGlobalLayout = true; // do global/default Layout event too

        UnityEngine.Camera _camera = null;
        RenderImGui _renderFeature = null;

        RenderType _rendererType = RenderType.Mesh;
        InputType _platformType = InputType.InputManager;

		private UIOConfig _initialConfiguration = new UIOConfig
		{
			ImGuiConfig = ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable,

			DoubleClickTime = 0.30f,
			DoubleClickMaxDist = 6.0f,

			DragThreshold = 6.0f,

			KeyRepeatDelay = 0.250f,
			KeyRepeatRate = 0.050f,

			FontGlobalScale = 1.0f,
			FontAllowUserScaling = false,

			DisplayFramebufferScale = Vector2.one,

			MouseDrawCursor = false,
			TextCursorBlink = false,

			ResizeFromEdges = true,
			MoveFromTitleOnly = true,
			ConfigMemoryCompactTimer = 1f,
		};
		
        FontAtlasConfigAsset _fontAtlasConfiguration = null;
        IniSettingsAsset _iniSettings = null;  // null: uses default imgui.ini file

        ShaderResourcesAsset _shaders = null;
        StyleAsset _style = null;
        CursorShapesAsset _cursorShapes = null;

        const string CommandBufferTag = "DearImGui";

        public DearImGui(UnityEngine.Camera camera)
        {
            _context = UImGuiUtility.CreateContext();
            _camera = camera;
            ForwardRendererData frd = Resources.Load<ForwardRendererData>("URP_Forward");
            _renderFeature = frd.rendererFeatures.First(x => x.name == "ImGuiFeature") as RenderImGui;
            _shaders = Resources.Load<ShaderResourcesAsset>("DefaultShader");
            _style = Resources.Load<StyleAsset>("DefaultStyle");
            _cursorShapes = Resources.Load<CursorShapesAsset>("DefaultCursorShape");
            
            if (_camera == null) 
                Fail(nameof(_camera));
            
            if (_renderFeature == null) 
                Fail(nameof(_renderFeature));

            _cmd = RenderUtility.GetCommandBuffer("UImGui");

            if (RenderUtility.IsUsingURP())
            {
				_renderFeature.Camera = _camera;
	            _renderFeature.CommandBuffer = _cmd;
            }
            else 
            {
	            _camera.AddCommandBuffer(CameraEvent.AfterEverything, _cmd);
            }

            UImGuiUtility.SetCurrentContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();

            _initialConfiguration.ApplyTo(io);
            _style?.ApplyTo(ImGui.GetStyle());

            _context.TextureManager.BuildFontAtlas(io, _fontAtlasConfiguration);
            _context.TextureManager.Initialize(io);

			IPlatform platform = PlatformUtility.Create(_platformType, _cursorShapes, _iniSettings);
			SetPlatform(platform, io);
			
            if (_platform == null)
			{
				Fail(nameof(_platform));
			}
            
			SetRenderer(RenderUtility.Create(_rendererType, _shaders, _context.TextureManager), io);
            if (_renderer == null)
            {
                Fail(nameof(_renderer));
            }

            void Fail(string reason)
            {
                throw new System.Exception($"Failed to start: {reason}");
            }
        }

        public override void OnTick()
        {
			UImGuiUtility.SetCurrentContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();
            _context.TextureManager.PrepareFrame(io);
            _platform.PrepareFrame(io, _camera.pixelRect);
            ImGui.NewFrame();

            try
            {
                if (_doGlobalLayout)
					UImGuiUtility.DoLayout();
                
                Layout?.Invoke();     // this.Layout: handlers specific to this instance
            }
            finally
            {
                ImGui.Render();
            }

            _cmd.Clear();
            _renderer.RenderDrawLists(_cmd, ImGui.GetDrawData());
        }
       
        public override void OnShutdown()
        {
			UImGuiUtility.SetCurrentContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();

            SetRenderer(null, io);
            SetPlatform(null, io);

			UImGuiUtility.SetCurrentContext(null);

			_context.TextureManager.Shutdown();
			_context.TextureManager.DestroyFontAtlas(io);

			if (RenderUtility.IsUsingURP())
			{
				if (_renderFeature != null)
				{
					_renderFeature.Camera = null;
					_renderFeature.CommandBuffer = null;
				}
			}
			else
			{
				if (_camera != null)
				{
					_camera.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmd);
				}
			}


            if (_cmd != null)
                RenderUtility.ReleaseCommandBuffer(_cmd);
            
            _cmd = null;
            
			UImGuiUtility.DestroyContext(_context);
        }
        
		private void Reset()
		{
			_camera = Engine.Camera.MainCamera;
			_initialConfiguration.SetDefaults();
		}


		private void SetRenderer(IRenderer renderer, ImGuiIOPtr io)
		{
			_renderer?.Shutdown(io);
			_renderer = renderer;
			_renderer?.Initialize(io);
		}

		private void SetPlatform(IPlatform platform, ImGuiIOPtr io)
		{
			_platform?.Shutdown(io);
			_platform = platform;
			_platform?.Initialize(io, _initialConfiguration, "Unity " + _platformType.ToString());
		}

    }
}
