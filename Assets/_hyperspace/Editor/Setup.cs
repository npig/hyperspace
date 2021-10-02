using System;
using UnityEditor;
using UnityEngine;

namespace Hyperspace.Editor
{
    public class SetupWindow : EditorWindow
    {
        private static readonly string ENABLE_SERVER = "EnableServer";
        private static readonly string ENABLE_UIDEV = "EnableUIDev";
        private bool _enableServer;
        private bool _enableImGuiDev;

        [MenuItem("Hyperspace/Config")]
        private static void Initialise()
        {
            SetupWindow window = (SetupWindow)EditorWindow.GetWindow(typeof(SetupWindow));
            window.Show();
        }

        private void OnEnable()
        {
            _enableServer = EditorPrefs.GetBool(ENABLE_SERVER);
            _enableImGuiDev = EditorPrefs.GetBool(ENABLE_UIDEV);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            _enableServer = EditorGUILayout.Toggle("Enable Server", _enableServer);
            _enableImGuiDev = EditorGUILayout.Toggle("Enable UI Dev", _enableImGuiDev);
            EditorGUILayout.EndVertical();
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(ENABLE_SERVER, _enableServer);
            EditorPrefs.SetBool(ENABLE_UIDEV, _enableImGuiDev);
        }
    }
}