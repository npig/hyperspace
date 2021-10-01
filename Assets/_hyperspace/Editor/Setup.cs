using System;
using UnityEditor;
using UnityEngine;

namespace Hyperspace.Editor
{
    public class SetupWindow : EditorWindow
    {
        private const string ENABLE_SERVER = "EnableServer";
        private bool _enableServer;

        [MenuItem("Hyperspace/Config")]
        private static void Initialise()
        {
            SetupWindow window = (SetupWindow)EditorWindow.GetWindow(typeof(SetupWindow));
            window.Show();
        }

        private void OnEnable()
        {
            _enableServer = EditorPrefs.GetBool(ENABLE_SERVER);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            _enableServer = EditorGUILayout.Toggle("Enable Server", _enableServer);
            EditorGUILayout.EndVertical();
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(ENABLE_SERVER, _enableServer);
        }
    }
}