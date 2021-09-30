using UnityEngine;

namespace ImGuiNET.Unity
{
    [CreateAssetMenu(menuName = "Dear ImGui/Font Atlas Configuration")]
    public sealed class FontAtlasConfigAsset : ScriptableObject
    {
        public FontRasterizerType Rasterizer;
        public uint RasterizerFlags;
        public FontDefinition[] Fonts;
    }

    public enum FontRasterizerType
    {
        StbTrueType,
        FreeType,
    }
}
