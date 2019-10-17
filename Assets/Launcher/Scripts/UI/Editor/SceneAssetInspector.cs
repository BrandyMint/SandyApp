using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Launcher.UI.Editor {
    public class SceneAssetInspector {
        public List<string> sceneNames = new List<string>();
        
        private class PropCache {
            public SerializedProperty prop;
            public SceneAsset oldScene;
        }
        private readonly Dictionary<string, PropCache> _oldValues = new Dictionary<string, PropCache>();
        
        
        public void CollectOldScenesValues(SerializedObject obj) {
            _oldValues.Clear();
            foreach (var name in sceneNames) {
                var propName = $"_scene{name}Path";
                var prop = obj.FindProperty(propName);
                if (prop != null) {
                    var propCahce = new PropCache {
                        prop = prop,
                        oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(prop.stringValue)
                    };
                    _oldValues.Add(name, propCahce);
                } else {
                    Debug.LogError($"not found property {propName}");
                }
            }
        }

        public void UpdateScenesChanges() {
            EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
            foreach (var name in sceneNames) {
                if (_oldValues.TryGetValue(name, out var propCache)) {
                    EditorGUI.BeginChangeCheck();
                    var newScene = EditorGUILayout.ObjectField(name, propCache.oldScene, typeof(SceneAsset), false) as SceneAsset;

                    if (EditorGUI.EndChangeCheck()) {
                        var newPath = AssetDatabase.GetAssetPath(newScene);
                        propCache.prop.stringValue = newPath;
                    }
                }
            }
        }

        public void OnInspectorGUI(SerializedObject obj) {
            CollectOldScenesValues(obj);

            obj.Update();
            
            UpdateScenesChanges();

            obj.ApplyModifiedProperties();
        }
    }
}