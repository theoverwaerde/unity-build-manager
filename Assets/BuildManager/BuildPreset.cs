using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildManager
{
    public class BuildPreset : ScriptableObject
    {
        public string presetName;
        public bool isActive = true;
        public BuildTarget buildTargets;
        public string[] extraScriptingDefines;
        public BuildOptions options;

        public string[] Scenes => EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        [ContextMenu("Reset to Standalone")]
        public BuildPreset SetStandalone()
        {
            presetName = "Standalone";
            buildTargets = BuildTarget.Windows | BuildTarget.Mac | BuildTarget.Linux;
            return this;
        }
        
        [ContextMenu("Reset to Mobile")]
        public BuildPreset SetMobile()
        {
            presetName = "Mobile";
            buildTargets = BuildTarget.IOS | BuildTarget.Android;
            return this;
        }
    }

    [CustomEditor(typeof(BuildPreset))]
    public class BuildToolSettingsDrawer : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Settings Window"))
            {
                BuildWindow.ShowWindow((BuildPreset)target);
            }
            EditorGUILayout.Space();
            
            base.OnInspectorGUI();
        }
    }
}