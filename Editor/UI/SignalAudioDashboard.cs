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
using System.Linq;

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

        [MenuItem("ResenhaTools/Signal/Dashboard")]
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
            
            TextField pathField = content.Q<TextField>("keys-path-field");
            if (pathField != null)
            {
                pathField.value = EditorPrefs.GetString("SignalAudio_KeysPath", "Default (Assets or Manager Location)");
            }

            Button changePathBtn = content.Q<Button>("btn-change-keys-path");
            if (changePathBtn != null)
            {
                changePathBtn.clicked += () =>
                {
                    string currentPath = EditorPrefs.GetString("SignalAudio_KeysPath", "Assets");
                    string defaultDir = System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(currentPath)) ? System.IO.Path.GetDirectoryName(currentPath) : "Assets";
                    
                    string newPath = EditorUtility.SaveFilePanelInProject(
                        "Select AudioKeys Export Location", 
                        "AudioKeys", 
                        "cs", 
                        "Choose where to save the generated AudioKeys script.", 
                        defaultDir);
                    
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        EditorPrefs.SetString("SignalAudio_KeysPath", newPath);
                        if (pathField != null) pathField.value = newPath;
                    }
                };
            }

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

            // Foldout state per list index (the true SerializedProperty index)
            var foldoutStates = new Dictionary<int, bool>();

            string currentSearchText = "";
            var searchField = content.Q<ToolbarSearchField>("audio-search-field");

            int currentPage = 0;
            int itemsPerPage = 10;
            List<int> filteredIndices = new List<int>();

            ListView listView = content.Q<ListView>("audio-list-view");
            Label lblPageInfo = content.Q<Label>("lbl-page-info");

            void UpdatePagination()
            {
                _serializedConfig.Update();
                SerializedProperty listProperty = _serializedConfig.FindProperty(propertyName);
                int totalItems = listProperty.arraySize;

                filteredIndices.Clear();

                // Build filtered list
                for (int i = 0; i < totalItems; i++)
                {
                    if (string.IsNullOrEmpty(currentSearchText))
                    {
                        filteredIndices.Add(i);
                    }
                    else
                    {
                        SerializedProperty itemProp = listProperty.GetArrayElementAtIndex(i);
                        SerializedProperty idProp = itemProp.FindPropertyRelative("audioID");
                        string id = idProp != null ? idProp.stringValue : "";
                        if (id.ToLowerInvariant().Contains(currentSearchText))
                        {
                            filteredIndices.Add(i);
                        }
                    }
                }

                int totalFiltered = filteredIndices.Count;
                int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalFiltered / itemsPerPage));

                if (currentPage >= totalPages) currentPage = totalPages - 1;
                if (currentPage < 0) currentPage = 0;

                if (lblPageInfo != null)
                {
                    lblPageInfo.text = $"Page {currentPage + 1} of {totalPages} (Total: {totalItems})";
                }

                // Get indices for current page
                var pageIndices = filteredIndices.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToList();
                
                // Set the list view items source to our small list of indices
                listView.itemsSource = pageIndices;
                listView.RefreshItems();
            }

            if (searchField != null)
            {
                searchField.RegisterValueChangedCallback(evt =>
                {
                    currentSearchText = evt.newValue?.ToLowerInvariant() ?? "";
                    currentPage = 0; // Reset to page 1 on search
                    UpdatePagination();
                });
            }

            // Pagination Controls
            var btnPrev = content.Q<Button>("btn-prev-page");
            var btnNext = content.Q<Button>("btn-next-page");
            var btnAdd = content.Q<Button>("btn-add-item");
            var btnRemove = content.Q<Button>("btn-remove-item");

            if (btnPrev != null)
            {
                btnPrev.clicked += () =>
                {
                    if (currentPage > 0)
                    {
                        currentPage--;
                        UpdatePagination();
                    }
                };
            }

            if (btnNext != null)
            {
                btnNext.clicked += () =>
                {
                    int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredIndices.Count / itemsPerPage));
                    if (currentPage < totalPages - 1)
                    {
                        currentPage++;
                        UpdatePagination();
                    }
                };
            }

            if (btnAdd != null)
            {
                btnAdd.clicked += () =>
                {
                    SerializedProperty listProperty = _serializedConfig.FindProperty(propertyName);
                    listProperty.arraySize++;
                    _serializedConfig.ApplyModifiedProperties();
                    
                    // Jump to last page
                    currentSearchText = "";
                    if (searchField != null) searchField.value = "";
                    
                    int newTotal = listProperty.arraySize;
                    currentPage = Mathf.Max(0, Mathf.CeilToInt((float)newTotal / itemsPerPage) - 1);
                    
                    UpdatePagination();
                    
                    // Scroll to bottom
                    listView.ScrollToItem(-1);
                };
            }

            if (btnRemove != null)
            {
                btnRemove.clicked += () =>
                {
                    if (listView.selectedIndex >= 0 && listView.selectedIndex < listView.itemsSource.Count)
                    {
                        int realIndex = (int)listView.itemsSource[listView.selectedIndex];
                        SerializedProperty listProperty = _serializedConfig.FindProperty(propertyName);
                        listProperty.DeleteArrayElementAtIndex(realIndex);
                        _serializedConfig.ApplyModifiedProperties();
                        
                        // Clear foldout state
                        if (foldoutStates.ContainsKey(realIndex)) foldoutStates.Remove(realIndex);
                        
                        UpdatePagination();
                    }
                };
            }

            BindListView(listView, propertyName, foldoutStates, (realIndex) => 
            {
                SerializedProperty listProp = _serializedConfig.FindProperty(propertyName);
                listProp.DeleteArrayElementAtIndex(realIndex);
                _serializedConfig.ApplyModifiedProperties();
                if (foldoutStates.ContainsKey(realIndex)) foldoutStates.Remove(realIndex);
                UpdatePagination();
            });

            // Initial load
            UpdatePagination();

            // Wire Collapse All button
            var btnCollapse = content.Q<Button>("btn-collapse-all");
            if (btnCollapse != null)
            {
                btnCollapse.clicked += () =>
                {
                    foldoutStates.Clear(); // all keys missing → default false
                    listView?.RefreshItems();
                };
            }

            // Wire Expand All button
            var btnExpand = content.Q<Button>("btn-expand-all");
            if (btnExpand != null)
            {
                btnExpand.clicked += () =>
                {
                    SerializedProperty prop = _serializedConfig.FindProperty(propertyName);
                    int count = prop?.arraySize ?? 0;
                    for (int i = 0; i < count; i++) foldoutStates[i] = true;
                    listView?.RefreshItems();
                };
            }

            // Wire Delete All button
            var btnDeleteAll = content.Q<Button>("btn-delete-all");
            if (btnDeleteAll != null)
            {
                btnDeleteAll.clicked += () =>
                {
                    if (EditorUtility.DisplayDialog("Delete All Audios", "Are you sure you want to delete ALL audio entries in this library? This cannot be undone.", "Yes, Delete All", "Cancel"))
                    {
                        SerializedProperty prop = _serializedConfig.FindProperty(propertyName);
                        prop.ClearArray();
                        _serializedConfig.ApplyModifiedProperties();
                        foldoutStates.Clear();
                        currentPage = 0;
                        UpdatePagination();
                    }
                };
            }

            content.Bind(_serializedConfig);
            _mainContent.Add(content);
        }

        private void LoadHelpTab()
        {
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset SignalHelpTab");
            if (guids.Length == 0) return;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var content = visualTree.Instantiate();


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

        private void ImportAudioClips(string folderPath, string prefix, string targetProperty)
        {
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(prefix)) return;

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
            
            _serializedConfig.Update();
            SoundManagerSO data = _serializedConfig.targetObject as SoundManagerSO;
            
            var targetList = targetProperty == "musicDatabase" ? data.musicDatabase : data.sfxDatabase;
            
            int clipsAdded = 0;
            int clipsUpdated = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                
                if (clip != null)
                {
                    string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                    
                    if (!filename.StartsWith(prefix)) continue;

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

        private void BindListView(ListView listView, string propertyName, Dictionary<int, bool> foldoutStates = null, System.Action<int> onDelete = null)
        {
            SerializedProperty listProperty = _serializedConfig.FindProperty(propertyName);
            
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
                        if (playBtn.userData is int realIdx)
                        {
                            _serializedConfig.ApplyModifiedProperties();
                            SoundManagerSO data = _serializedConfig.targetObject as SoundManagerSO;
                            var targetList = propertyName == "musicDatabase" ? data.musicDatabase : data.sfxDatabase;
                            
                            if (realIdx >= 0 && realIdx < targetList.Count)
                            {
                                PlayPreview(targetList[realIdx]);
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
                Button deleteBtn = element.Q<Button>("btn-delete");
                if (deleteBtn != null)
                {
                    deleteBtn.clicked += () =>
                    {
                        if (deleteBtn.userData is int realIdx)
                        {
                            if (EditorUtility.DisplayDialog("Delete Audio", "Are you sure you want to delete this audio entry?", "Yes", "No"))
                            {
                                onDelete?.Invoke(realIdx);
                            }
                        }
                    };
                }
                return element;
            };

            listView.bindItem = (element, pageIndex) =>
            {
                // Our itemsSource is a List<int> containing the real indices
                if (pageIndex < 0 || pageIndex >= listView.itemsSource.Count) return;
                
                int realIndex = (int)listView.itemsSource[pageIndex];

                if (realIndex < 0 || realIndex >= listProperty.arraySize) return;

                SerializedProperty itemProperty = listProperty.GetArrayElementAtIndex(realIndex);

                var idField = element.Q<TextField>("audio-id-field");
                idField?.BindProperty(itemProperty.FindPropertyRelative("audioID"));

                // Force white text
                if (idField != null)
                {
                    var textInput = idField.Q<VisualElement>(className: "unity-base-text-field__input");
                    if (textInput != null) textInput.style.color = Color.white;
                }

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

                var spatialBlendField = element.Q<PropertyField>("spatial-blend-field");
                spatialBlendField?.BindProperty(itemProperty.FindPropertyRelative("spatialBlend"));

                var dopplerLevelField = element.Q<PropertyField>("doppler-level-field");
                dopplerLevelField?.BindProperty(itemProperty.FindPropertyRelative("dopplerLevel"));

                var spreadField = element.Q<PropertyField>("spread-field");
                spreadField?.BindProperty(itemProperty.FindPropertyRelative("spread"));

                var minDistanceField = element.Q<PropertyField>("min-distance-field");
                minDistanceField?.BindProperty(itemProperty.FindPropertyRelative("minDistance"));

                var maxDistanceField = element.Q<PropertyField>("max-distance-field");
                maxDistanceField?.BindProperty(itemProperty.FindPropertyRelative("maxDistance"));

                var playBtn = element.Q<Button>("btn-play");
                if (playBtn != null) playBtn.userData = realIndex;
                var deleteBtn = element.Q<Button>("btn-delete");
                if (deleteBtn != null) deleteBtn.userData = realIndex;

                // Restore foldout state
                var foldout = element.Q<Foldout>();
                if (foldout != null)
                {
                    if (foldout.userData is EventCallback<ChangeEvent<bool>> prevCb)
                        foldout.UnregisterValueChangedCallback(prevCb);

                    bool isExpanded = foldoutStates != null && foldoutStates.TryGetValue(realIndex, out bool saved) ? saved : false;
                    foldout.SetValueWithoutNotify(isExpanded);

                    EventCallback<ChangeEvent<bool>> cb = evt =>
                    {
                        if (foldoutStates != null) foldoutStates[realIndex] = evt.newValue;
                    };
                    foldout.userData = cb;
                    foldout.RegisterValueChangedCallback(cb);
                }

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
                        
                        if (realIndex >= listProperty.arraySize) return;

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
                                if (textInput != null) textInput.style.color = Color.white;
                            }
                        }
                    }
                    catch (System.Exception)
                    {

                    }
                }

                ValidateEntry();

                if (element.userData is IVisualElementScheduledItem oldSchedule)
                {
                    oldSchedule.Pause();
                }

                IVisualElementScheduledItem scheduledValidation = element.schedule.Execute(ValidateEntry).Every(250);
                element.userData = scheduledValidation;
                
                element.RegisterCallback<DetachFromPanelEvent>(_ => 
                {
                    if (element.userData is IVisualElementScheduledItem sched)
                        sched.Pause();
                });
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