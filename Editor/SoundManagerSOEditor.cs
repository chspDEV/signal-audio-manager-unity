using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace SignalAudioManager
{
    [CustomEditor(typeof(SoundManagerSO))]
    public class SoundManagerSOEditor : Editor
    {
        private SerializedProperty audioMixerProp;
        private SerializedProperty masterVolumeParamProp;
        private SerializedProperty musicVolumeParamProp;
        private SerializedProperty sfxVolumeParamProp;
        private SerializedProperty uiVolumeParamProp;
        private SerializedProperty sfxPrefabProp;
        private SerializedProperty maxSimultaneousMusicProp;
        private SerializedProperty sfxPoolSizeProp;
        private SerializedProperty musicDatabaseProp;
        private SerializedProperty sfxDatabaseProp;

        private bool showMusicDatabase = true;
        private bool showSfxDatabase = true;
        private DefaultAsset importFolderMusic = null;
        private string importPrefixMusic = "ost_";
        private DefaultAsset importFolderSfx = null;
        private string importPrefixSfx = "sfx_";

        private GUIStyle headerStyle;
        private GUIStyle sectionHeaderStyle;

        private void OnEnable()
        {
            audioMixerProp = serializedObject.FindProperty("audioMixer");
            masterVolumeParamProp = serializedObject.FindProperty("masterVolumeParam");
            musicVolumeParamProp = serializedObject.FindProperty("musicVolumeParam");
            sfxVolumeParamProp = serializedObject.FindProperty("sfxVolumeParam");
            uiVolumeParamProp = serializedObject.FindProperty("uiVolumeParam");
            sfxPrefabProp = serializedObject.FindProperty("sfxPrefab");
            maxSimultaneousMusicProp = serializedObject.FindProperty("maxSimultaneousMusic");
            sfxPoolSizeProp = serializedObject.FindProperty("sfxPoolSize");
            musicDatabaseProp = serializedObject.FindProperty("musicDatabase");
            sfxDatabaseProp = serializedObject.FindProperty("sfxDatabase");
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();
            serializedObject.Update();

            DrawSection("Audio Mixer Configuration", () =>
            {
                EditorGUILayout.PropertyField(audioMixerProp);
                EditorGUILayout.PropertyField(masterVolumeParamProp);
                EditorGUILayout.PropertyField(musicVolumeParamProp);
                EditorGUILayout.PropertyField(sfxVolumeParamProp);
                EditorGUILayout.PropertyField(uiVolumeParamProp);
            });

            DrawSection("Asset References", () =>
            {
                EditorGUILayout.PropertyField(sfxPrefabProp);
            });

            DrawSection("General Settings", () =>
            {
                EditorGUILayout.PropertyField(maxSimultaneousMusicProp);
                EditorGUILayout.PropertyField(sfxPoolSizeProp);
            });

            EditorGUILayout.Space(15);
            DrawSeparator();
            EditorGUILayout.Space(10);

            SoundManagerSO data = (SoundManagerSO)target;

            DrawDatabaseSection(
                ref showMusicDatabase,
                "🎵  Music Audio Database",
                new Color(0.4f, 0.6f, 1f, 0.25f),
                () => {
                    EditorGUILayout.PropertyField(musicDatabaseProp, true);
                    DrawImporter(data, data.musicDatabase, ref importFolderMusic, ref importPrefixMusic);
                }
            );

            EditorGUILayout.Space(10);

            DrawDatabaseSection(
                ref showSfxDatabase,
                "🔊  SFX/UI Audio Database",
                new Color(1f, 0.6f, 0.4f, 0.25f),
                () => {
                    EditorGUILayout.PropertyField(sfxDatabaseProp, true);
                    DrawImporter(data, data.sfxDatabase, ref importFolderSfx, ref importPrefixSfx);
                }
            );

            serializedObject.ApplyModifiedProperties();
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 0, 0, 0)
                };
            }

            if (sectionHeaderStyle == null)
            {
                sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11
                };
            }
        }

        private void DrawSection(string title, System.Action drawContent)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, sectionHeaderStyle);
            EditorGUI.indentLevel++;
            drawContent?.Invoke();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        private void DrawDatabaseSection(ref bool foldout, string title, Color headerColor, System.Action drawContent)
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, 28);
            EditorGUI.DrawRect(headerRect, headerColor);

            Rect labelRect = new Rect(headerRect.x + 8, headerRect.y, headerRect.width - 8, headerRect.height);
            foldout = EditorGUI.BeginFoldoutHeaderGroup(labelRect, foldout, title, headerStyle);
            EditorGUI.EndFoldoutHeaderGroup();

            if (foldout)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space(8);
                drawContent?.Invoke();
                EditorGUILayout.Space(8);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawImporter(SoundManagerSO data, List<AudioEntry> targetList, ref DefaultAsset importFolder, ref string importPrefix)
        {
            EditorGUILayout.Space(12);

            Rect toolHeaderRect = GUILayoutUtility.GetRect(0, 26);
            EditorGUI.DrawRect(toolHeaderRect, new Color(0.25f, 0.25f, 0.25f, 0.4f));

            GUIStyle toolHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0)
            };

            GUI.Label(toolHeaderRect, "📁  Audio Importer Tool", toolHeaderStyle);

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            importFolder = (DefaultAsset)EditorGUILayout.ObjectField("Source Folder", importFolder, typeof(DefaultAsset), false);
            importPrefix = EditorGUILayout.TextField("File Prefix", importPrefix);

            EditorGUILayout.Space(8);

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.45f);

            if (GUILayout.Button("Import Audio Clips from Folder", GUILayout.Height(28)))
            {
                ImportAudioClips(data, targetList, importFolder, importPrefix);
            }

            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndVertical();
        }

        private void ImportAudioClips(SoundManagerSO data, List<AudioEntry> targetList, DefaultAsset importFolder, string importPrefix)
        {
            if (importFolder == null)
            {
                EditorUtility.DisplayDialog("Import Error", "Please assign a source folder first.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(importFolder);
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });

            int clipsAdded = 0;
            int clipsUpdated = 0;

            foreach (string guid in audioGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(assetPath);

                if (filename.StartsWith(importPrefix))
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip == null) continue;

                    string audioID = filename.Substring(importPrefix.Length);

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
                EditorUtility.DisplayDialog("Import Successful!", $"✓ Added {clipsAdded} new entries\n✓ Updated {clipsUpdated} existing entries", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No New Clips Found", $"No new audio clips with prefix '{importPrefix}' were found.", "OK");
            }
        }

        private void DrawSeparator()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 2f);
            rect.xMin = 0f;
            rect.width += 4f;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }
    }
}