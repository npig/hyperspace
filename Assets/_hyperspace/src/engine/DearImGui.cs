using System.Linq;
using ImGuiNET;
using ImGuiNET.Unity;
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
        ImGuiUnityContext _context;
        IImGuiRenderer _renderer;
        IImGuiPlatform _platform;
        CommandBuffer _cmd;
        bool _usingURP;

        public event System.Action Layout;  // Layout event for *this* ImGui instance
        bool _doGlobalLayout = true; // do global/default Layout event too

        UnityEngine.Camera _camera = null;
        RenderImGuiFeature _renderFeature = null;

        RenderUtils.RenderType _rendererType = RenderUtils.RenderType.Mesh;
        ImGuiNET.Unity.Platform.Type _platformType = ImGuiNET.Unity.Platform.Type.InputManager;

        IOConfig _initialConfiguration = default;
        FontAtlasConfigAsset _fontAtlasConfiguration = null;
        IniSettingsAsset _iniSettings = null;  // null: uses default imgui.ini file

        ShaderResourcesAsset _shaders = null;
        StyleAsset _style = null;
        CursorShapesAsset _cursorShapes = null;

        const string CommandBufferTag = "DearImGui";

        public DearImGui(UnityEngine.Camera camera)
        {
            _context = ImGuiUn.CreateUnityContext();
            _camera = camera;
            ForwardRendererData frd = Resources.Load<ForwardRendererData>("URP_Forward");
            _renderFeature = frd.rendererFeatures.First(x => x.name == "ImGuiFeature") as RenderImGuiFeature;
            _shaders = Resources.Load<ShaderResourcesAsset>("DefaultShaderResources");
            _style = Resources.Load<StyleAsset>("DefaultStyle");
            _cursorShapes = Resources.Load<CursorShapesAsset>("DefaultCursorShapes");
            _usingURP = RenderUtils.IsUsingURP();
           _initialConfiguration.SetDefaults();
            
            if (_camera == null) 
                Fail(nameof(_camera));
            
            if (_renderFeature == null && _usingURP) 
                Fail(nameof(_renderFeature));

            _cmd = RenderUtils.GetCommandBuffer(CommandBufferTag);
            
            if (_usingURP)
                _renderFeature.commandBuffer = _cmd;
            else
                _camera.AddCommandBuffer(CameraEvent.AfterEverything, _cmd);

            ImGuiUn.SetUnityContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();

            _initialConfiguration.ApplyTo(io);
            _style?.ApplyTo(ImGui.GetStyle());

            _context.textures.BuildFontAtlas(io, _fontAtlasConfiguration);
            _context.textures.Initialize(io);

            SetPlatform(ImGuiNET.Unity.Platform.Create(_platformType, _cursorShapes, _iniSettings), io);
            SetRenderer(RenderUtils.Create(_rendererType, _shaders, _context.textures), io);
            
            if (_platform == null) 
                Fail(nameof(_platform));
            
            if (_renderer == null) 
                Fail(nameof(_renderer));

            void Fail(string reason)
            {
                throw new System.Exception($"Failed to start: {reason}");
            }
        }


        public override void OnTick()
        {
            ImGuiUn.SetUnityContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();

            _context.textures.PrepareFrame(io);
            _platform.PrepareFrame(io, _camera.pixelRect);
            ImGui.NewFrame();

            try
            {
                if (_doGlobalLayout)
                    ImGuiUn.DoLayout();   // ImGuiUn.Layout: global handlers
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
            ImGuiUn.SetUnityContext(_context);
            ImGuiIOPtr io = ImGui.GetIO();

            SetRenderer(null, io);
            SetPlatform(null, io);

            ImGuiUn.SetUnityContext(null);

            _context.textures.Shutdown();
            _context.textures.DestroyFontAtlas(io);

            if (_usingURP)
            {
                if (_renderFeature != null)
                    _renderFeature.commandBuffer = null;
            }
            else
            {
                if (_camera != null)
                    _camera.RemoveCommandBuffer(CameraEvent.AfterEverything, _cmd);
            }

            if (_cmd != null)
                RenderUtils.ReleaseCommandBuffer(_cmd);
            
            _cmd = null;
            
            ImGuiUn.DestroyUnityContext(_context);
        }
        
        private void Reset()
        {
            _camera = UnityEngine.Camera.main;
            _initialConfiguration.SetDefaults();
        }

        private void SetRenderer(IImGuiRenderer renderer, ImGuiIOPtr io)
        {
            _renderer?.Shutdown(io);
            _renderer = renderer;
            _renderer?.Initialize(io);
        }

       private void SetPlatform(IImGuiPlatform platform, ImGuiIOPtr io)
        {
            _platform?.Shutdown(io);
            _platform = platform;
            _platform?.Initialize(io);
        }
    }
}
