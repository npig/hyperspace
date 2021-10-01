using ImGuiNET;
using System;
using UImGui.Texture;
using UnityEngine;
using UTexture = UnityEngine.Texture;

namespace UImGui
{
	public static class UImGuiUtility
	{
		public static IntPtr GetTextureId(UTexture texture) => Context?.TextureManager.GetTextureId(texture) ?? IntPtr.Zero;
		internal static SpriteInfo GetSpriteInfo(Sprite sprite) => Context?.TextureManager.GetSpriteInfo(sprite) ?? null;

		internal static Context Context;

		#region Events
		public static event Action Layout;
		public static event Action OnInitialize;
		public static event Action OnDeinitialize;
		public static void DoLayout() => Layout?.Invoke();
		internal static void DoOnInitialize() => OnInitialize?.Invoke();
		internal static void DoOnDeinitialize() => OnDeinitialize?.Invoke();
		#endregion

		public static unsafe Context CreateContext()
		{
			return new Context
			{
				ImGuiContext = ImGui.CreateContext(),
#if !UIMGUI_REMOVE_IMPLOT
				ImPlotContext = ImPlotNET.ImPlot.CreateContext(),
#endif
#if !UIMGUI_REMOVE_IMNODES
				ImNodesContext = new IntPtr(imnodesNET.imnodes.CreateContext()),
#endif
				TextureManager = new TextureManager()
			};
		}

		public static void DestroyContext(Context context)
		{
			ImGui.DestroyContext(context.ImGuiContext);

#if !UIMGUI_REMOVE_IMPLOT
			ImPlotNET.ImPlot.DestroyContext(context.ImPlotContext);
#endif
#if !UIMGUI_REMOVE_IMNODES
			imnodesNET.imnodes.DestroyContext(context.ImNodesContext);
#endif
		}

		public static void SetCurrentContext(Context context)
		{
			Context = context;
			ImGui.SetCurrentContext(context?.ImGuiContext ?? IntPtr.Zero);

#if !UIMGUI_REMOVE_IMPLOT
			ImPlotNET.ImPlot.SetImGuiContext(context?.ImGuiContext ?? IntPtr.Zero);
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
			ImGuizmoNET.ImGuizmo.SetImGuiContext(context?.ImGuiContext ?? IntPtr.Zero);
#endif
#if !UIMGUI_REMOVE_IMNODES
			imnodesNET.imnodes.SetImGuiContext(context?.ImGuiContext ?? IntPtr.Zero);
#endif
		}
	}
}