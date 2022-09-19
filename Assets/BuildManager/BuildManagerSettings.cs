using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace BuildManager
{
	[FilePath("ProjectSettings/"+nameof(BuildManagerSettings)+".asset", FilePathAttribute.Location.ProjectFolder)]
	public class BuildManagerSettings : ScriptableSingleton<BuildManagerSettings>
	{
		public string buildPath = "Builds/{preset}/{platform}/{product}";
		public bool zipBuilds = false;
		public string zipPath = "Builds/{preset}/{product}_{platform}_{version}.zip";
		public CompressionLevel zipCompressionLevel = CompressionLevel.Optimal;
		public bool zipContainsFolder = true;
		public bool incrementBuildVersionByBuild = false;
		public byte incrementVersion = 2;
		public bool splitMacOSBuilds = false;
		public bool stopBuildOnErrors = false;
		//public bool confirmOverwrite = true;

		internal SerializedObject GetSerializedObject()
		{
			return new SerializedObject(this);
		}
		
		public void Save()
		{
			Save(true);
		}

		private void OnDisable()
		{
			Save();
		}
	}

	internal class BuildManagerSettingsProvider : SettingsProvider
	{
		private SerializedObject _serializedObject;
		private SerializedProperty _buildPath;
		private SerializedProperty _zipBuilds;
		private SerializedProperty _zipPath;
		private SerializedProperty _zipCompressionLevel;
		private SerializedProperty _zipContainsFolder;
		private SerializedProperty _incrementBuildVersionByBuild;
		private SerializedProperty _incrementVersion;
		private SerializedProperty _splitMacOSBuilds;
		private SerializedProperty _stopBuildOnErrors;
		//private SerializedProperty _confirmOverwrite;

	    private class Styles
	    {
	        public static readonly GUIContent BuildPathLabel = EditorGUIUtility.TrTextContent("Build Path", "The path to save the build to. Use {PresetName}, {PlatformName} and {BuildName} to insert the preset name, platform name and build name.");
	        public static readonly GUIContent ZipBuildsLabel = EditorGUIUtility.TrTextContent("Zip Builds", "If enabled, the build will be zipped after it has been built.");
	        public static readonly GUIContent ZipPathLabel = EditorGUIUtility.TrTextContent("Zip Path", "The path to save the zip to. Use {PresetName}, {PlatformName} and {BuildName} to insert the preset name, platform name and build name.");
	        public static readonly GUIContent ZipCompressionLevelLabel = EditorGUIUtility.TrTextContent("Zip Compression Level", "The compression level to use when zipping the build.");
	        public static readonly GUIContent ZipContainsFolderLabel = EditorGUIUtility.TrTextContent("Zip Contains Folder", "If enabled, the zip will contain a folder with the build name.");
	        public static readonly GUIContent IncrementBuildVersionByBuildLabel = EditorGUIUtility.TrTextContent("Increment Build Version", "If enabled, the build version will be incremented by 1 after each build.");
	        public static readonly GUIContent IncrementVersionMajorLabel = EditorGUIUtility.TrTextContent("Major", "If checked, the major version will be incremented by 1 for each build.");
	        public static readonly GUIContent IncrementVersionMinorLabel = EditorGUIUtility.TrTextContent("Minor", "If checked, the minor version will be incremented by 1 for each build.");
	        public static readonly GUIContent IncrementVersionPatchLabel = EditorGUIUtility.TrTextContent("Patch", "If checked, the patch version will be incremented by 1 for each build.");
	        public static readonly GUIContent SplitMacOSBuildsLabel = EditorGUIUtility.TrTextContent("Split macOS Builds", "If enabled, the macOS build will be split into a Intel and a Silicon build.");
	        public static readonly GUIContent StopBuildOnErrorsLabel = EditorGUIUtility.TrTextContent("Stop Build On Errors", "If enabled, the build will stop if there are any errors.");
	        //public static readonly GUIContent ConfirmOverwriteLabel = EditorGUIUtility.TrTextContent("Confirm Overwrite", "If enabled, the build will ask for confirmation before overwriting an existing build.");
	    }

	    private BuildManagerSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
	        : base(path, scopes, keywords) { }

	    public override void OnActivate(string searchContext, VisualElement rootElement)
	    {
		    BuildManagerSettings.instance.Save();
	        _serializedObject = BuildManagerSettings.instance.GetSerializedObject();
	        _buildPath = _serializedObject.FindProperty("buildPath");
	        _zipBuilds = _serializedObject.FindProperty("zipBuilds");
	        _zipPath = _serializedObject.FindProperty("zipPath");
	        _zipCompressionLevel = _serializedObject.FindProperty("zipCompressionLevel");
	        _zipContainsFolder = _serializedObject.FindProperty("zipContainsFolder");
	        _incrementBuildVersionByBuild = _serializedObject.FindProperty("incrementBuildVersionByBuild");
	        _incrementVersion = _serializedObject.FindProperty("incrementVersion");
	        _splitMacOSBuilds = _serializedObject.FindProperty("splitMacOSBuilds");
	        _stopBuildOnErrors = _serializedObject.FindProperty("stopBuildOnErrors");
	        //_confirmOverwrite = _serializedObject.FindProperty("confirmOverwrite");
	    }

	    public override void OnGUI(string searchContext)
	    {
            _serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            _buildPath.stringValue = EditorGUILayout.TextField(Styles.BuildPathLabel, _buildPath.stringValue);
            
            _zipBuilds.boolValue = EditorGUILayout.BeginToggleGroup(Styles.ZipBuildsLabel, _zipBuilds.boolValue);
            _zipPath.stringValue = EditorGUILayout.TextField(Styles.ZipPathLabel, _zipPath.stringValue);
            _zipCompressionLevel.intValue = (int)(CompressionLevel)EditorGUILayout.EnumPopup(Styles.ZipCompressionLevelLabel, (CompressionLevel)_zipCompressionLevel.intValue);
            _zipContainsFolder.boolValue = EditorGUILayout.Toggle(Styles.ZipContainsFolderLabel, _zipContainsFolder.boolValue);
            EditorGUILayout.EndToggleGroup();
            
            _incrementBuildVersionByBuild.boolValue = EditorGUILayout.BeginToggleGroup(Styles.IncrementBuildVersionByBuildLabel, _incrementBuildVersionByBuild.boolValue);
            EditorGUILayout.BeginHorizontal();
            _incrementVersion.intValue = EditorGUILayout.Toggle(Styles.IncrementVersionMajorLabel, _incrementVersion.intValue == 0) ? 0 : _incrementVersion.intValue;
            _incrementVersion.intValue = EditorGUILayout.Toggle(Styles.IncrementVersionMinorLabel, _incrementVersion.intValue == 1) ? 1 : _incrementVersion.intValue;
            _incrementVersion.intValue = EditorGUILayout.Toggle(Styles.IncrementVersionPatchLabel, _incrementVersion.intValue == 2) ? 2 : _incrementVersion.intValue;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            
            _splitMacOSBuilds.boolValue = EditorGUILayout.Toggle(Styles.SplitMacOSBuildsLabel, _splitMacOSBuilds.boolValue);
            _stopBuildOnErrors.boolValue = EditorGUILayout.Toggle(Styles.StopBuildOnErrorsLabel, _stopBuildOnErrors.boolValue);
            //_confirmOverwrite.boolValue = EditorGUILayout.Toggle(Styles.ConfirmOverwriteLabel, _confirmOverwrite.boolValue);
            
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                BuildManagerSettings.instance.Save();
            }
	    }

	    [SettingsProvider]
	    public static SettingsProvider CreateTimelineProjectSettingProvider()
	    {
	        BuildManagerSettingsProvider provider = new BuildManagerSettingsProvider("Project/Build Manager", SettingsScope.Project, GetSearchKeywordsFromGUIContentProperties<Styles>());
	        return provider;
	    }
	}
}