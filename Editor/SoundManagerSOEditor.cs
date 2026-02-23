/* ==============================================================================
 * CLASS: SoundManagerSOEditor
 * DESCRIPTION: Custom Inspector for the SoundManagerSO. Prevents raw data editing
 * and redirects the user to the modern UI Toolkit Dashboard for a better UX.
 * ==============================================================================*/
using UnityEditor;
using UnityEngine;
using SignalAudioManagerUnity.Data; 
using SignalAudioManagerUnity.Editor.UI; 

namespace SignalAudioManagerUnity.Editor
{
    [CustomEditor(typeof(SoundManagerSO))]
    public class SoundManagerSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(15);
            
            EditorGUILayout.HelpBox(
                "Signal Audio Manager uses a dedicated, high-performance UI Toolkit Dashboard. " +
                "Please use the Dashboard to manage your audio libraries, import tools, and global settings safely.", 
                MessageType.Info);
            
            EditorGUILayout.Space(15);
            
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.18f, 0.42f, 0.2f); 
            
            if (GUILayout.Button("🚀 Open Signal Dashboard", GUILayout.Height(45)))
            {
                SignalAudioDashboard.ShowWindow();
            }
            
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.Space(15);
        }
    }
}