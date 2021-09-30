using UnityEditor;
using UnityEngine;

namespace Hyperspace.Editor
{
    public class SetupWindow : EditorWindow
    {
        private bool _enableServer = false;

        [MenuItem("Hyperspace/Config")]
        private static void Initialise()
        {
            SetupWindow window = (SetupWindow)EditorWindow.GetWindow(typeof(SetupWindow));
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            _enableServer = EditorGUILayout.Toggle("Enable Server", _enableServer);
            EditorGUILayout.EndVertical();
        } 
    }
}