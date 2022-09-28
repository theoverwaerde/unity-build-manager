using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BuildManager.Scripts
{
    internal sealed partial class BuildWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset buildManager;
        [SerializeField] private VisualTreeAsset presetLine;
        [SerializeField] private VisualTreeAsset modalRemove;
        [SerializeField] private VisualTreeAsset buildPreset;
        
        private readonly List<BuildPreset> _presets = new List<BuildPreset>();
        private const string DefaultPath = "Assets/Editor/BuildManager/Preset.asset";
        
        private Label _presetCount;
        private Button _addPreset;
        private Button _removePreset;
        private ListView _buildPresets;
        private HelpBox _helpBox;
        private Label _buildVersion;
        private Label _buildPath;
        private Button _build;
        private Button _openBuildFolder;
        private EnumField _specificBuildTarget;
        private Button _specificBuild;
        private Button _forceSpecificBuild;
        private Label _progress;
        private ProgressBar _progressBar;
        
        private VisualElement _mainMenu;
        private VisualElement _editMenu;

        [MenuItem("File/Build Manager... %&B", priority = 206)]
        public static void ShowWindow()
        {
            BuildWindow window = GetWindow<BuildWindow>();
            window.titleContent = new GUIContent("Build Manager");

            window._editMenu?.RemoveFromHierarchy();
        }
        
        public static void ShowWindow(BuildPreset preset)
        {
            ShowWindow();
            GetWindow<BuildWindow>().EditPreset(preset);
        }

        public void CreateGUI()
        {
            _mainMenu = buildManager.Instantiate();
            rootVisualElement.Add(_mainMenu);
            
            _mainMenu.style.flexGrow = 1;
            
            _presetCount = _mainMenu.Q<Label>("Count");
            _addPreset = _mainMenu.Q<Button>("Create");
            _removePreset = _mainMenu.Q<Button>("Remove");
            _buildPresets = _mainMenu.Q<ListView>("Presets");
            _helpBox = _mainMenu.Q<HelpBox>("HelpBox");
            _buildVersion = _mainMenu.Q<Label>("Version");
            _buildPath = _mainMenu.Q<Label>("Location");
            _build = _mainMenu.Q<Button>("Build");
            _openBuildFolder = _mainMenu.Q<Button>("BuildFolder");
            _specificBuildTarget = _mainMenu.Q<EnumField>("Platform");
            _specificBuild = _mainMenu.Q<Button>("BuildPlatform");
            _forceSpecificBuild = _mainMenu.Q<Button>("ForceBuild");
            _progress = _mainMenu.Q<Label>("ProgressText");
            _progressBar = _mainMenu.Q<ProgressBar>("ProgressBar");
            
            #if UNITY_EDITOR_WIN
            _specificBuildTarget.Init(BuildTarget.Windows);
            #elif UNITY_EDITOR_OSX
            _specificBuildTarget.Init(BuildTarget.Mac);
            #elif UNITY_EDITOR_LINUX
            _specificBuildTarget.Init(BuildTarget.Linux);
            #endif
            _specificBuildTarget.RegisterValueChangedCallback(_ => UpdateGenericData());
            
            _removePreset.SetEnabled(false);
            
            _addPreset.clicked += AddPreset;
            _removePreset.clicked += RemovePreset;
            _build.clicked += Build;
            _openBuildFolder.clicked += OpenBuildFolder;
            _specificBuild.clicked += BuildSpecific;
            _forceSpecificBuild.clicked += BuildSpecific;
            
            _buildPresets.itemsSource = _presets;
            _buildPresets.makeItem = MakeItem;
            _buildPresets.bindItem = BindItem;
            _buildPresets.onSelectionChange += _ => UpdateGenericData();
            _buildPresets.Rebuild();
            
            VisualElement MakeItem()
            {
                VisualElement item = presetLine.CloneTree();
                
                item.style.flexDirection = FlexDirection.Row;
                item.style.alignItems = Align.Center;

                item.Q<Toggle>().RegisterCallback<ChangeEvent<bool>>(_ => UpdateGenericData());
                item.Q<Button>("Edit").clickable.clickedWithEventInfo += EditPreset;
                
                return item;
            }
            
            void BindItem(VisualElement e, int i)
            {
                BuildPreset preset = _presets[i];
                
                e.Bind(new SerializedObject(preset));
                e.Q<PlatformIcon>().value = preset.buildTargets;
                
                e.viewDataKey = i.ToString();
            }
            
            LoadPresets();
            _buildPresets.RefreshItems();
        }
        
        private void AddPreset()
        {
            string folderPath = DefaultPath;
            
            if(_presets.Count > 0)
            {
                folderPath = AssetDatabase.GetAssetPath(_presets[0]);
            }
            
            folderPath = folderPath[..folderPath.LastIndexOf('/')];
            
            string defaultName = DefaultPath[(DefaultPath.LastIndexOf('/') + 1)..];
            string path = folderPath + "/" + defaultName;
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(folderPath[..folderPath.LastIndexOf('/')], folderPath[(folderPath.LastIndexOf('/') + 1)..]);
            }
            
            BuildPreset preset = CreateInstance<BuildPreset>().SetStandalone();
            
            AssetDatabase.CreateAsset(preset, AssetDatabase.GenerateUniqueAssetPath(path));
            
            _presets.Add(preset);
            
            _buildPresets.RefreshItems();
        }
        
        private void RemovePreset()
        {
            object obj = _buildPresets.selectedItem;
            
            if (obj is not BuildPreset preset)
            {
                return;
            }

            VisualElement rootFromUxml = modalRemove.Instantiate();
            rootVisualElement.Add(rootFromUxml);
            
            rootFromUxml.style.position = Position.Absolute;
            rootFromUxml.style.top = 0;
            rootFromUxml.style.left = 0;
            rootFromUxml.style.right = 0;
            rootFromUxml.style.bottom = 0;
            
            rootFromUxml.Q<Label>("Name").text = preset.presetName;
            rootFromUxml.Q<Button>("Cancel").clicked += Cancel;
            rootFromUxml.Q<Button>("Confirm").clicked += Confirm;

            void Cancel()
            {
                rootVisualElement.Remove(rootFromUxml);
            }

            void Confirm()
            {
                string presetPath = AssetDatabase.GetAssetPath(preset);
                
                _presets.Remove(preset);

                AssetDatabase.DeleteAsset(presetPath);
            
                _buildPresets.RefreshItems();
                
                Cancel();
            }
        }
        
        private static void OpenBuildFolder()
        {
            string path = BuildManagerSettings.instance.buildPath;
            
            path = path[..path.IndexOf('{')];
            
            path = Application.dataPath.Replace("Assets", path);
            
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            Process.Start(path);
        }

        private void UpdateGenericData()
        {
            _presetCount.text = $"Preset Count: <b>{_presets.Count}</b>";
            _buildVersion.text = $"Build Version: <b>{PlayerSettings.bundleVersion}</b>";
            
            _buildPath.text = $"Build Path: <b>{CompilePath(CreateInstance<BuildPreset>().SetStandalone(), (BuildTarget)_specificBuildTarget.value)}</b>";
            
            bool hasSelection = _buildPresets.selectedIndex != -1;
            
            _removePreset.SetEnabled(hasSelection);

            BuildTarget specificBuild = (BuildTarget)_specificBuildTarget.value;
            
            bool specificBuildSupport = hasSelection && VerifyBuildModule(specificBuild);
            bool specificBuildSupportAvailable = specificBuildSupport && (specificBuild & ((BuildPreset)_buildPresets.selectedItem).buildTargets) != 0;

            const string noModule = "This build target is not installed.";
            const string noInPreset = "This build target is not supported by the preset.";
            
            _specificBuild.SetEnabled(specificBuildSupportAvailable);
            _forceSpecificBuild.SetEnabled(specificBuildSupport);
            
            _forceSpecificBuild.tooltip = specificBuildSupport ? "" : noModule;
            _specificBuild.tooltip = specificBuildSupportAvailable ? "" : specificBuildSupport ? noInPreset : noModule;
            
            BuildTarget globalBuildTargets = 0;

            foreach (BuildPreset preset in _presets)
            {
                if (preset.isActive)
                {
                    globalBuildTargets |= preset.buildTargets;
                }
            }

            BuildTarget canBuild = VerifyBuildModules(globalBuildTargets);
            
            byte buildCount = 0;
            foreach (BuildPreset preset in _presets)
            {
                if (preset.isActive)
                {
                    buildCount += (byte)Count(preset.buildTargets & canBuild);
                }
            }
            
            _build.SetEnabled(buildCount != 0);
            
            _progress.text = $"Build Progress: <b>0/{buildCount}</b>";
        }

        private static uint Count(BuildTarget targets)
        {
            uint v = (uint)targets;
            v -= (v >> 1) & 0x55555555; // reuse input as temporary
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333); // temp
            uint c = ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
            return c;
        }
        
        private void LoadPresets()
        {
            string[] assets = AssetDatabase.FindAssets("t:" + nameof(BuildPreset));

            if (assets.Length == 0)
            {
                AddPreset();
                
                UpdateGenericData();
                
                return;
            }
            
            _presets.Clear();

            foreach(string path in assets)
            {
                _presets.Add(AssetDatabase.LoadAssetAtPath<BuildPreset>(AssetDatabase.GUIDToAssetPath(path)));
            }
            
            UpdateGenericData();
        }

        private void EditPreset(EventBase obj)
        {
            int index = int.Parse(((VisualElement)obj.currentTarget).parent.viewDataKey);
            
            EditPreset(_presets[index]);
        }

        private void EditPreset(BuildPreset preset)
        {
            _mainMenu.style.display = DisplayStyle.None;
            _editMenu = buildPreset.Instantiate();
            rootVisualElement.Add(_editMenu);

            _editMenu.Bind(new SerializedObject(preset));
            
            _editMenu.Q<Button>("Back").clicked += BackToManage;
            
            _editMenu.Q<TextField>("Name").RegisterValueChangedCallback(UpdateName);
            
            void BackToManage()
            {
                rootVisualElement.Remove(_editMenu);
                _mainMenu.style.display = DisplayStyle.Flex;
            }
            
            void UpdateName(ChangeEvent<string> evt)
            {
                string path = AssetDatabase.GetAssetPath(preset);
                AssetDatabase.RenameAsset(path, evt.newValue);
            }
        }
    }
}
