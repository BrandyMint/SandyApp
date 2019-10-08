﻿using UnityEditor;

namespace Launcher.UI.Editor {
    [CustomEditor(typeof(Scenes), true)]
    public class ScenesEditor : UnityEditor.Editor {
        SceneAssetInspector scenes = new SceneAssetInspector {
            sceneNames = {
                "ProjectorParams",
                "Calibration"
            }
        };
        
        public override void OnInspectorGUI() {
            scenes.OnInspectorGUI(serializedObject);
        }
    }
}