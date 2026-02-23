/* ==============================================================================
 * CLASS: SignalAudioDashboard
 * DESCRIPTION: Main Editor Window. Handles 1-Click Initial Setup generation,
 * tab navigation, automated data binding, and audio preview playback.
 * ==============================================================================*/

using SignalAudioManagerUnity.Core;
using SignalAudioManagerUnity.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace SignalAudioManagerUnity.Editor.UI
{
    public class SignalAudioDashboard : EditorWindow
    {
        private VisualElement _dashboardContainer;
        private VisualElement _welcomeScreen;
        private VisualElement _mainContent;
        private UQueryState<Button> _sidebarButtons;
        private SerializedObject _serializedConfig;
        
        private AudioSource _previewSource;
        private GameObject _previewObject;

        [MenuItem("Window/Signal Audio/Dashboard")]
        public static void ShowWindow()
        {
            SignalAudioDashboard wnd = GetWindow<SignalAudioDashboard>();
            wnd.titleContent = new GUIContent("Signal Dashboard");
            wnd.minSize = new Vector2(800, 500); 
        }

        private void OnEnable()
        {
            LoadSerializedConfiguration();
            CreatePreviewAudioSource();
        }

        private void OnDisable()
        {
            if (_previewObject != null)
            {
                DestroyImmediate(_previewObject);
            }
        }

        private void CreatePreviewAudioSource()
        {
            if (_previewObject == null)
            {
                _previewObject = new GameObject("SignalAudio_Preview_Hidden");
                _previewObject.hideFlags = HideFlags.HideAndDontSave;
                _previewSource = _previewObject.AddComponent<AudioSource>();
                _previewSource.playOnAwake = false;
            }
        }

        private void LoadSerializedConfiguration()
        {
            string[] guids = AssetDatabase.FindAssets("t:SoundManagerSO");
            if (guids.Length > 0)
            {
                var configAsset = AssetDatabase.LoadAssetAtPath<SoundManagerSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (configAsset != null)
                {
                    _serializedConfig = new SerializedObject(configAsset);
                }
            }
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            string[] uxmlGuids = AssetDatabase.FindAssets("t:VisualTreeAsset SignalAudioDashboard");
            if (uxmlGuids.Length == 0) return;
            
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(uxmlGuids[0]));
            visualTree.CloneTree(root);

            string[] ussGuids = AssetDatabase.FindAssets("t:StyleSheet SignalStyle");
            if (ussGuids.Length > 0)
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(ussGuids[0]));
                root.styleSheets.Add(styleSheet);
            }

            _dashboardContainer = root.Q<VisualElement>("dashboard-container");
            _welcomeScreen = root.Q<VisualElement>("welcome-screen");

            Button setupBtn = root.Q<Button>("btn-initial-setup");
            if (setupBtn != null) setupBtn.clicked += RunInitialSetup;

            InitializeTabs(root);
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            if (_serializedConfig == null)
            {
                _dashboardContainer.style.display = DisplayStyle.None;
                _welcomeScreen.style.display = DisplayStyle.Flex;
            }
            else
            {
                _dashboardContainer.style.display = DisplayStyle.Flex;
                _welcomeScreen.style.display = DisplayStyle.None;
                
                var firstBtn = _dashboardContainer.Q<Button>("tab-global");
                if (firstBtn != null) LoadTabContent(firstBtn.name);
            }
        }

        private void InitializeTabs(VisualElement root)
        {
            _mainContent = root.Q<VisualElement>("main-content");
            _sidebarButtons = root.Query<Button>(className: "sidebar-btn").Build();

            foreach (var button in _sidebarButtons)
            {
                button.clicked += () => OnTabClicked(button);
            }
        }

        private void OnTabClicked(Button clickedButton)
        {
            foreach (var button in _sidebarButtons)
            {
                button.RemoveFromClassList("sidebar-btn-active");
            }

            clickedButton.AddToClassList("sidebar-btn-active");
            LoadTabContent(clickedButton.name);
        }

        private void LoadTabContent(string tabName)
        {
            if (_mainContent == null || _serializedConfig == null) return;
            
            _mainContent.Clear();
            _serializedConfig.Update();

            switch (tabName)
            {
                case "tab-global":
                    LoadGlobalSettingsTab();
                    break;
                case "tab-music":
                    LoadLibraryTab("Music Library Management", "musicDatabase");
                    break;
                case "tab-sfx":
                    LoadLibraryTab("SFX & UI Audio Management", "sfxDatabase");
                    break;
                case "tab-tools":
                    _mainContent.Add(new Label("Audio Import Tools (Coming Soon)"));
                    break;
            }
        }

        private void LoadGlobalSettingsTab()
        {
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset SignalGlobalSettings");
            if (guids.Length == 0) return;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var content = visualTree.Instantiate();
            content.Bind(_serializedConfig);
            _mainContent.Add(content);
        }

        private void LoadLibraryTab(string title, string propertyName)
        {
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset SignalAudioLibrary");
            if (guids.Length == 0) return;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var content = visualTree.Instantiate();
            content.style.flexGrow = 1;

            content.Q<Label>("library-title").text = title;

            ListView listView = content.Q<ListView>("audio-list-view");
            BindListView(listView, propertyName);
            
            content.Bind(_serializedConfig);

            _mainContent.Add(content);
        }

        private void BindListView(ListView listView, string propertyName)
        {
            SerializedProperty listProperty = _serializedConfig.FindProperty(propertyName);
            listView.bindingPath = propertyName;

            string[] itemGuids = AssetDatabase.FindAssets("t:VisualTreeAsset AudioEntryItem");
            if (itemGuids.Length == 0) return;

            var itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(itemGuids[0]));

            listView.makeItem = () =>
            {
                var element = itemTemplate.Instantiate();

                Button playBtn = element.Q<Button>("btn-play");
                Button stopBtn = element.Q<Button>("btn-stop");

                if (playBtn != null)
                {
                    playBtn.clicked += () =>
                    {
                        if (playBtn.userData is int idx)
                        {
                            SerializedProperty prop = _serializedConfig.FindProperty(propertyName).GetArrayElementAtIndex(idx);
                            PlayPreview(prop);
                        }
                    };
                }

                if (stopBtn != null)
                {
                    stopBtn.clicked += () =>
                    {
                        if (_previewSource != null) _previewSource.Stop();
                    };
                }

                return element;
            };

            listView.bindItem = (element, index) =>
            {
                if (index < 0 || index >= listProperty.arraySize) return;

                SerializedProperty itemProperty = listProperty.GetArrayElementAtIndex(index);
                
                var idField = element.Q<TextField>("audio-id-field");
                if (idField != null) idField.BindProperty(itemProperty.FindPropertyRelative("audioID"));

                var clipsField = element.Q<PropertyField>("audio-clips-field");
                if (clipsField != null) clipsField.BindProperty(itemProperty.FindPropertyRelative("audioClips"));

                var minVolField = element.Q<PropertyField>("min-vol-field");
                if (minVolField != null) minVolField.BindProperty(itemProperty.FindPropertyRelative("minVolume"));

                var maxVolField = element.Q<PropertyField>("max-vol-field");
                if (maxVolField != null) maxVolField.BindProperty(itemProperty.FindPropertyRelative("maxVolume"));

                var minPitchField = element.Q<PropertyField>("min-pitch-field");
                if (minPitchField != null) minPitchField.BindProperty(itemProperty.FindPropertyRelative("minPitch"));

                var maxPitchField = element.Q<PropertyField>("max-pitch-field");
                if (maxPitchField != null) maxPitchField.BindProperty(itemProperty.FindPropertyRelative("maxPitch"));

                Button playBtn = element.Q<Button>("btn-play");
                if (playBtn != null) playBtn.userData = index;
            };
        }

        private void PlayPreview(SerializedProperty entryProperty)
        {
            if (_previewSource == null) return;
            _previewSource.Stop();

            SerializedProperty clipsProp = entryProperty.FindPropertyRelative("audioClips");
            if (clipsProp == null || clipsProp.arraySize == 0) return;

            int randomIndex = Random.Range(0, clipsProp.arraySize);
            SerializedProperty clipProp = clipsProp.GetArrayElementAtIndex(randomIndex);
            
            AudioClip clip = clipProp.objectReferenceValue as AudioClip;
            if (clip == null) return;

            float minPitch = entryProperty.FindPropertyRelative("minPitch").floatValue;
            float maxPitch = entryProperty.FindPropertyRelative("maxPitch").floatValue;
            float minVol = entryProperty.FindPropertyRelative("minVolume").floatValue;
            float maxVol = entryProperty.FindPropertyRelative("maxVolume").floatValue;

            _previewSource.clip = clip;
            _previewSource.pitch = Random.Range(minPitch, maxPitch);
            _previewSource.volume = Random.Range(minVol, maxVol);
            _previewSource.Play();
        }

        private void RunInitialSetup()
        {
            // The setup logic remains exactly the same as previously implemented
            string targetBaseFolder = "Assets/Resenha Studio";
            string targetFolder = "Assets/Resenha Studio/SignalAudio_Setup";
            
            if (!AssetDatabase.IsValidFolder(targetBaseFolder))
                AssetDatabase.CreateFolder("Assets", "Resenha Studio");
                
            if (!AssetDatabase.IsValidFolder(targetFolder))
                AssetDatabase.CreateFolder(targetBaseFolder, "SignalAudio_Setup");

            string mixerGuid = FindTemplateGuid("SignalAudioMixer", "t:AudioMixerController");
            string sfxPrefabGuid = FindTemplateGuid("SFX_Prefab", "t:Prefab");
            string managerPrefabGuid = FindTemplateGuid("Audio_Managers", "t:Prefab");

            if (string.IsNullOrEmpty(mixerGuid) || string.IsNullOrEmpty(sfxPrefabGuid))
            {
                EditorUtility.DisplayDialog("Setup Error", "Templates not found in the package.", "OK");
                return;
            }

            string newMixerPath = $"{targetFolder}/SignalAudioMixer.mixer";
            string newSfxPath = $"{targetFolder}/SFX_Prefab.prefab";
            string newManagerPath = $"{targetFolder}/Audio_Managers.prefab";

            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(mixerGuid), newMixerPath);
            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(sfxPrefabGuid), newSfxPath);
            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(managerPrefabGuid), newManagerPath);

            SoundManagerSO newSettings = ScriptableObject.CreateInstance<SoundManagerSO>();
            newSettings.audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(newMixerPath);
            newSettings.sfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newSfxPath);
            newSettings.masterVolumeParam = "MasterVolume";
            newSettings.musicVolumeParam = "MusicVolume";
            newSettings.sfxVolumeParam = "SFXVolume";
            newSettings.uiVolumeParam = "UIVolume";
            newSettings.maxSimultaneousMusic = 3;
            newSettings.sfxPoolSize = 20;

            string settingsPath = $"{targetFolder}/Project_AudioSettings.asset";
            AssetDatabase.CreateAsset(newSettings, settingsPath);

            GameObject managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newManagerPath);
            if (managerPrefab != null)
            {
                var soundManager = managerPrefab.GetComponent<SoundManager>();
                if (soundManager != null)
                {
                    using (var so = new SerializedObject(soundManager))
                    {
                        so.FindProperty("_config").objectReferenceValue = newSettings;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SavePrefabAsset(managerPrefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadSerializedConfiguration();
            UpdateUIState();
        }

        private string FindTemplateGuid(string fileName, string filter)
        {
            string[] guids = AssetDatabase.FindAssets($"{fileName} {filter}");
            return guids.Length > 0 ? guids[0] : null;
        }
    }
}