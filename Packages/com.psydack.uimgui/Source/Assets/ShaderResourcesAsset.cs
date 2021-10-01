using UnityEngine;

namespace UImGui.Assets
{
	[CreateAssetMenu(menuName = "Dear ImGui/Shader Resources")]
	public sealed class ShaderResourcesAsset : ScriptableObject
	{
		public ShaderData Shader;
		public ShaderProperties PropertyNames;
	}
}
