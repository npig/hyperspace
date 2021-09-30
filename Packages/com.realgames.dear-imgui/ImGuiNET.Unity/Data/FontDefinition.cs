using UnityEngine;

namespace ImGuiNET.Unity
{
    [System.Serializable]
    public struct FontDefinition
    {
        [SerializeField] Object _fontAsset; // to drag'n'drop file from the inspector
        public string FontPath;
        public FontConfig Config;
    }
}
