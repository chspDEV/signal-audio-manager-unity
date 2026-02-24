/* ==============================================================================
 * CLASS: SignalAudioDashboard
 * DESCRIPTION: Main Editor Window. Handles 1-Click Initial Setup generation,
 * tab navigation, automated data binding, and audio preview playback.
 * ==============================================================================*/

using System.Collections.Generic;
using System.IO;
using SignalAudioManagerUnity.Core;
using SignalAudioManagerUnity.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System.Text;
using System.Text.RegularExpressions;

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
            
            root.schedule.Execute(() => {
                CheckResponsivity(root.resolvedStyle.width);
            }).StartingIn(100);

            Button setupBtn = root.Q<Button>("btn-initial-setup");
            if (setupBtn != null) setupBtn.clicked += RunInitialSetupWithFolderPicker;

            InitializeTabs(root);
            UpdateUIState();
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        private void RunInitialSetupWithFolderPicker()
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Setup Folder", "Assets", "");
    
            if (string.IsNullOrEmpty(selectedPath)) return;
            
            if (selectedPath.StartsWith(Application.dataPath)) {
                selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            } else {
                EditorUtility.DisplayDialog("Error", "Please select a folder inside your Assets directory.", "OK");
                return;
            }

            RunInitialSetup(selectedPath);
        }
        
        private void CheckResponsivity(float width)
        {
            if (_dashboardContainer == null) return;
            if (width < 600f) _dashboardContainer.AddToClassList("collapsed-mode");
            else _dashboardContainer.RemoveFromClassList("collapsed-mode");
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_dashboardContainer == null) return;
            
            if (evt.newRect.width < 600f)
            {
                _dashboardContainer.AddToClassList("collapsed-mode");
            }
            else
            {
                _dashboardContainer.RemoveFromClassList("collapsed-mode");
            }
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
            
            SetIcon(root.Q<VisualElement>("icon-global"), "icon_settings");
            SetIcon(root.Q<VisualElement>("icon-music"), "icon_music");
            SetIcon(root.Q<VisualElement>("icon-sfx"), "icon_sfx");
            SetIcon(root.Q<VisualElement>("icon-tools"), "icon_import");
            SetIcon(root.Q<VisualElement>("icon-help"), "icon_help");

            foreach (var button in _sidebarButtons)
            {
                button.clicked += () => OnTabClicked(button);
            }
        }

        private void SetIcon(VisualElement element, string iconName)
        {
            if (element == null) return;
            string[] guids = AssetDatabase.FindAssets($"{iconName} t:Texture2D");
            if (guids.Length > 0)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                element.style.backgroundImage = new StyleBackground(tex);
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
                    LoadImportToolsTab();
                    break;
                case "tab-help":
                    LoadHelpTab();
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
            
            Button generateBtn = content.Q<Button>("btn-generate-keys");
            if (generateBtn != null)
            {
                generateBtn.clicked += GenerateAudioKeysScript;
            }

            _mainContent.Add(content);
        }
        
        // ==========================================
        // CODE GENERATOR (AUDIO KEYS)
        // ==========================================
        private void GenerateAudioKeysScript()
        {
            if (_serializedConfig == null || _serializedConfig.targetObject == null) return;

            SoundManagerSO data = _serializedConfig.targetObject as SoundManagerSO;
            
            string filePath = EditorPrefs.GetString("SignalAudio_KeysPath", "");
            
            if (string.IsNullOrEmpty(filePath) || !System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                string defaultDir = "Assets";
                string soPath = AssetDatabase.GetAssetPath(data);
                if (!string.IsNullOrEmpty(soPath)) 
                {
                    defaultDir = System.IO.Path.GetDirectoryName(soPath);
                }

                filePath = EditorUtility.SaveFilePanelInProject(
                    "Save AudioKeys Script", 
                    "AudioKeys", 
                    "cs", 
                    "Choose where to save the generated AudioKeys script.", 
                    defaultDir);
                
                if (string.IsNullOrEmpty(filePath)) return;

                EditorPrefs.SetString("SignalAudio_KeysPath", filePath);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/* ==============================================================================");
            sb.AppendLine(" * AUTO-GENERATED BY SIGNAL AUDIO MANAGER");
            sb.AppendLine(" * DESCRIPTION: Strongly-typed audio IDs to prevent typos and enable autocomplete.");
            sb.AppendLine(" * Do not modify this file manually. Regenerate it from the Signal Dashboard.");
            sb.AppendLine(" * ==============================================================================*/");
            sb.AppendLine();
            sb.AppendLine("namespace SignalAudioManagerUnity");
            sb.AppendLine("{");
            sb.AppendLine("    public static class AudioKeys");
            sb.AppendLine("    {");

            // --- SFX CLASS ---
            sb.AppendLine("        public static class SFX");
            sb.AppendLine("        {");
            foreach (var entry in data.sfxDatabase)
            {
                if (string.IsNullOrWhiteSpace(entry.audioID)) continue;
                string safeName = SanitizeVariableName(entry.audioID);
                sb.AppendLine($"            public const string {safeName} = \"{entry.audioID}\";");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // --- MUSIC CLASS ---
            sb.AppendLine("        public static class Music");
            sb.AppendLine("        {");
            foreach (var entry in data.musicDatabase)
            {
                if (string.IsNullOrWhiteSpace(entry.audioID)) continue;
                string safeName = SanitizeVariableName(entry.audioID);
                sb.AppendLine($"            public const string {safeName} = \"{entry.audioID}\";");
            }
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            System.IO.File.WriteAllText(filePath, sb.ToString());
            
            AssetDatabase.Refresh(); 
            
            EditorUtility.DisplayDialog("Success", $"AudioKeys.cs generated successfully at:\n{filePath}\n\nYou can now use AudioKeys.SFX.your_sound in your scripts.", "OK");
        }
        
        private string SanitizeVariableName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "_empty";
            
            string clean = Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
            
            if (char.IsDigit(clean[0]))
            {
                clean = "_" + clean;
            }
            return clean;
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

        private void LoadHelpTab()
        {
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset SignalHelpTab");
            if (guids.Length == 0) return;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var content = visualTree.Instantiate();

            Button pdfBtn = content.Q<Button>("btn-open-pdf");
            if (pdfBtn != null)
            {
                pdfBtn.clicked += () => 
                {
                    string[] pdfGuids = AssetDatabase.FindAssets("Signal_Audio_Manager_Doc");
            
                    if (pdfGuids.Length > 0)
                    {
                        string relativePath = AssetDatabase.GUIDToAssetPath(pdfGuids[0]);
                        
                        string absolutePath = System.IO.Path.GetFullPath(relativePath);
                        
                        EditorUtility.OpenWithDefaultApp(absolutePath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Documentation PDF not found in the project.", "OK");
                    }
                };
            }

            _mainContent.Add(content);
        }

        private void LoadImportToolsTab()
        {
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset SignalImportTools");
            if (guids.Length == 0) return;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var content = visualTree.Instantiate();

            // MUSIC CARD
            ObjectField folderMusic = content.Q<ObjectField>("folder-picker-music");
            folderMusic.objectType = typeof(DefaultAsset);
            TextField prefixMusic = content.Q<TextField>("prefix-input-music");
            Button btnMusic = content.Q<Button>("btn-run-import-music");

            string savedPathMusic = EditorPrefs.GetString("SignalAudio_ImportPath_Music", "");
            if (!string.IsNullOrEmpty(savedPathMusic)) folderMusic.value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(savedPathMusic);
            prefixMusic.value = EditorPrefs.GetString("SignalAudio_ImportPrefix_Music", "ost_");

            btnMusic.clicked += () => RunBatchImport("Music", folderMusic.value as DefaultAsset, prefixMusic.value);

            // SFX CARD
            ObjectField folderSfx = content.Q<ObjectField>("folder-picker-sfx");
            folderSfx.objectType = typeof(DefaultAsset);
            TextField prefixSfx = content.Q<TextField>("prefix-input-sfx");
            Button btnSfx = content.Q<Button>("btn-run-import-sfx");

            string savedPathSfx = EditorPrefs.GetString("SignalAudio_ImportPath_SFX", "");
            if (!string.IsNullOrEmpty(savedPathSfx)) folderSfx.value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(savedPathSfx);
            prefixSfx.value = EditorPrefs.GetString("SignalAudio_ImportPrefix_SFX", "sfx_");

            btnSfx.clicked += () => RunBatchImport("SFX", folderSfx.value as DefaultAsset, prefixSfx.value);
            
            // BATCH RENAMER CARD
            ObjectField folderRename = content.Q<ObjectField>("folder-picker-rename");
            folderRename.objectType = typeof(DefaultAsset);
            TextField prefixRename = content.Q<TextField>("prefix-input-rename");
            Button btnRename = content.Q<Button>("btn-run-rename");

            string savedPathRename = EditorPrefs.GetString("SignalAudio_RenamePath", "");
            if (!string.IsNullOrEmpty(savedPathRename)) folderRename.value = AssetDatabase.LoadAssetAtPath<DefaultAsset>(savedPathRename);
            prefixRename.value = EditorPrefs.GetString("SignalAudio_RenamePrefix", "sfx_");

            btnRename.clicked += () => RunBatchRename(folderRename.value as DefaultAsset, prefixRename.value);

            _mainContent.Add(content);
        }
        
        private void RunBatchRename(DefaultAsset targetFolder, string prefix)
        {
            if (targetFolder == null)
            {
                EditorUtility.DisplayDialog("Rename Error", "Please assign a target folder first.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                EditorUtility.DisplayDialog("Rename Error", "The Prefix cannot be empty.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(targetFolder);
            EditorPrefs.SetString("SignalAudio_RenamePath", path);
            EditorPrefs.SetString("SignalAudio_RenamePrefix", prefix);

            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
            int renamedCount = 0;

            foreach (string guid in audioGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(assetPath);
                
                if (!filename.StartsWith(prefix))
                {
                    string newName = prefix + filename;
                    AssetDatabase.RenameAsset(assetPath, newName);
                    renamedCount++;
                }
            }

            if (renamedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Rename Successful!", $"✓ Successfully renamed {renamedCount} audio files.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Files Renamed", $"All audio files in this folder already start with '{prefix}', or no audio files were found.", "OK");
            }
        }

        private void RunBatchImport(string targetDB, DefaultAsset importFolder, string prefix)
        {
            if (importFolder == null)
            {
                EditorUtility.DisplayDialog("Import Error", "Please assign a source folder first.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                EditorUtility.DisplayDialog("Import Error", "The File Prefix is required to identify and group the audio files.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(importFolder);
            EditorPrefs.SetString($"SignalAudio_ImportPath_{targetDB}", path);
            EditorPrefs.SetString($"SignalAudio_ImportPrefix_{targetDB}", prefix);

            SoundManagerSO data = _serializedConfig.targetObject as SoundManagerSO;
            List<AudioEntry> targetList = targetDB == "Music" ? data.musicDatabase : data.sfxDatabase;

            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });

            int clipsAdded = 0;
            int clipsUpdated = 0;

            foreach (string guid in audioGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(assetPath);

                if (filename.StartsWith(prefix))
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip == null) continue;

                    string audioID = filename.Substring(prefix.Length);

                    AudioEntry existingEntry = targetList.Find(entry => entry.audioID == audioID);
                    if (existingEntry != null)
                    {
                        if (!existingEntry.audioClips.Contains(clip))
                        {
                            existingEntry.audioClips.Add(clip);
                            clipsUpdated++;
                        }
                    }
                    else
                    {
                        AudioEntry newEntry = new AudioEntry { audioID = audioID };
                        newEntry.audioClips.Add(clip);
                        targetList.Add(newEntry);
                        clipsAdded++;
                    }
                }
            }

            if (clipsAdded > 0 || clipsUpdated > 0)
            {
                EditorUtility.SetDirty(data);
                _serializedConfig.Update();
                EditorUtility.DisplayDialog("Import Successful!", $"✓ Added {clipsAdded} new entries\n✓ Updated {clipsUpdated} existing entries", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No New Clips Found", $"No new audio clips starting with '{prefix}' were found in the selected folder.", "OK");
            }
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
                            _serializedConfig.ApplyModifiedProperties();
                            SoundManagerSO data = _serializedConfig.targetObject as SoundManagerSO;
                            var targetList = propertyName == "musicDatabase" ? data.musicDatabase : data.sfxDatabase;
                            
                            if (idx >= 0 && idx < targetList.Count)
                            {
                                PlayPreview(targetList[idx]);
                            }
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
                idField?.BindProperty(itemProperty.FindPropertyRelative("audioID"));

                var clipsField = element.Q<PropertyField>("audio-clips-field");
                clipsField?.BindProperty(itemProperty.FindPropertyRelative("audioClips"));

                var minVolField = element.Q<PropertyField>("min-vol-field");
                minVolField?.BindProperty(itemProperty.FindPropertyRelative("minVolume"));

                var maxVolField = element.Q<PropertyField>("max-vol-field");
                maxVolField?.BindProperty(itemProperty.FindPropertyRelative("maxVolume"));

                var minPitchField = element.Q<PropertyField>("min-pitch-field");
                minPitchField?.BindProperty(itemProperty.FindPropertyRelative("minPitch"));

                var maxPitchField = element.Q<PropertyField>("max-pitch-field");
                maxPitchField?.BindProperty(itemProperty.FindPropertyRelative("maxPitch"));

                var playBtn = element.Q<Button>("btn-play");
                if (playBtn != null) playBtn.userData = index;

                // --- NOVO SISTEMA DE BANNER DE ERRO ---
                var errorContainer = element.Q<VisualElement>("error-container");
                Label errorLabel = null;

                if (errorContainer == null)
                {
                    errorContainer = new VisualElement { name = "error-container" };
                    errorContainer.style.flexDirection = FlexDirection.Row;
                    errorContainer.style.alignItems = Align.Center;
                    errorContainer.style.backgroundColor = new Color(0.25f, 0.08f, 0.08f, 0.9f); 
                    errorContainer.style.paddingTop = 6;
                    errorContainer.style.paddingBottom = 6;
                    errorContainer.style.paddingLeft = 10;
                    errorContainer.style.paddingRight = 10;
                    errorContainer.style.marginTop = 10;
                    errorContainer.style.borderBottomLeftRadius = 6;
                    errorContainer.style.borderBottomRightRadius = 6;
                    errorContainer.style.borderTopRightRadius = 6;
                    errorContainer.style.borderTopLeftRadius = 6;
                    errorContainer.style.borderLeftWidth = 3;
                    errorContainer.style.borderLeftColor = new Color(1f, 0.3f, 0.3f);
                    errorContainer.style.display = DisplayStyle.None;

                    var icon = new VisualElement { name = "error-icon" };
                    icon.style.width = 16;
                    icon.style.height = 16;
                    icon.style.marginRight = 8;
                    icon.style.unityBackgroundImageTintColor = new Color(1f, 0.4f, 0.4f);
                    SetIcon(icon, "icon_error");

                    errorLabel = new Label { name = "error-label" };
                    errorLabel.style.color = new Color(1f, 0.7f, 0.7f);
                    errorLabel.style.fontSize = 12;
                    errorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                    errorContainer.Add(icon);
                    errorContainer.Add(errorLabel);
                    
                    element.Insert(0, errorContainer);
                }
                else
                {
                    errorLabel = errorContainer.Q<Label>("error-label");
                }

                void ValidateEntry()
                {
                    try
                    {
                        if (itemProperty == null || itemProperty.serializedObject == null) return;
                        
                        if (index >= listProperty.arraySize) return;

                        itemProperty.serializedObject.Update();
                        
                        var idProp = itemProperty.FindPropertyRelative("audioID");
                        if (idProp == null) return;

                        string id = idProp.stringValue;
                        SerializedProperty clipsProp = itemProperty.FindPropertyRelative("audioClips");
                        
                        bool hasValidClip = false;
                        if (clipsProp != null && clipsProp.isArray)
                        {
                            for (int i = 0; i < clipsProp.arraySize; i++)
                            {
                                var elementProp = clipsProp.GetArrayElementAtIndex(i);
                                if (elementProp != null && elementProp.objectReferenceValue != null)
                                {
                                    hasValidClip = true;
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(id) || !hasValidClip)
                        {
                            errorContainer.style.display = DisplayStyle.Flex;
                            
                            if (errorLabel != null)
                            {
                                errorLabel.text = string.IsNullOrEmpty(id) 
                                    ? "Critical: Audio ID cannot be empty!" 
                                    : "Critical: No valid AudioClips assigned to this ID!";
                            }
                            
                            if (idField != null)
                            {
                                var textInput = idField.Q<VisualElement>(className: "unity-base-text-field__input");
                                if (textInput != null) textInput.style.color = new Color(1f, 0.4f, 0.4f);
                            }
                        }
                        else
                        {
                            errorContainer.style.display = DisplayStyle.None;
                            
                            if (idField != null)
                            {
                                var textInput = idField.Q<VisualElement>(className: "unity-base-text-field__input");
                                if (textInput != null) textInput.style.color = StyleKeyword.Null;
                            }
                        }
                    }
                    catch (System.Exception)
                    {

                    }
                }

                ValidateEntry();
                element.schedule.Execute(ValidateEntry).Every(250);
            };
        }

        private void PlayPreview(AudioEntry entry)
        {
            if (_previewSource == null || entry == null) return;
            _previewSource.Stop();

            AudioClip clip = entry.GetRandomClip();
            if (clip == null) return;

            _previewSource.clip = clip;
            _previewSource.pitch = entry.GetRandomPitch();
            _previewSource.volume = entry.GetRandomVolume();
            
            _previewSource.Play();
        }

       private void RunInitialSetup(string selectedPath)
        {
            string targetBaseFolder = !string.IsNullOrEmpty(selectedPath) ? selectedPath : "Assets/Resenha Studio";
            string targetFolder = $"{targetBaseFolder}/SignalAudio_Setup";
            string resourcesFolder = $"{targetFolder}/Resources";
            
            string absoluteBasePath = Application.dataPath + targetBaseFolder.Substring(6);
            string absoluteTargetFolder = Application.dataPath + targetFolder.Substring(6);
            string absoluteResourcesFolder = Application.dataPath + resourcesFolder.Substring(6);
            
            if (!Directory.Exists(absoluteBasePath)) Directory.CreateDirectory(absoluteBasePath);
            if (!Directory.Exists(absoluteTargetFolder)) Directory.CreateDirectory(absoluteTargetFolder);
            if (!Directory.Exists(absoluteResourcesFolder)) Directory.CreateDirectory(absoluteResourcesFolder);
            
            AssetDatabase.Refresh();

            string mixerGuid = FindTemplateGuid("SignalAudioMixer", "t:AudioMixerController");
            string sfxPrefabGuid = FindTemplateGuid("SFX_Prefab", "t:Prefab");
            string managerPrefabGuid = FindTemplateGuid("Audio_Managers", "t:Prefab");

            if (string.IsNullOrEmpty(mixerGuid) || string.IsNullOrEmpty(sfxPrefabGuid) || string.IsNullOrEmpty(managerPrefabGuid))
            {
                EditorUtility.DisplayDialog("Setup Error", "Templates not found in the package.", "OK");
                return;
            }

            string newMixerPath = $"{targetFolder}/SignalAudioMixer.mixer";
            string newSfxPath = $"{targetFolder}/SFX_Prefab.prefab";
            string newManagerPath = $"{resourcesFolder}/Audio_Managers.prefab";

            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(mixerGuid), newMixerPath);
            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(sfxPrefabGuid), newSfxPath);
            AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(managerPrefabGuid), newManagerPath);

            SoundManagerSO newSettings = ScriptableObject.CreateInstance<SoundManagerSO>();
            
            AudioMixer loadedMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(newMixerPath);
            GameObject loadedSfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newSfxPath);

            if (loadedMixer != null) newSettings.audioMixer = loadedMixer;
            if (loadedSfxPrefab != null) newSettings.sfxPrefab = loadedSfxPrefab;

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
            else
            {
                EditorUtility.DisplayDialog("Setup Warning", "Audio_Managers prefab could not be loaded for configuration. Please check the Resources folder.", "OK");
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