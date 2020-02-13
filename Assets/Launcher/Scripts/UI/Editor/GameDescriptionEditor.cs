using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utilities.Editor;

namespace Launcher.UI.Editor {
    [CustomEditor(typeof(GameDescription), true)]
    public class GameDescriptionEditor : UnityEditor.Editor {
        SceneAssetInspector scenes = new SceneAssetInspector {
            sceneNames = {""}
        };
        
        public override void OnInspectorGUI() {
            scenes.OnInspectorGUI(serializedObject);
            DrawDefaultInspector ();
        }
        
        [MenuItem("Assets/Create Game Description", true)]
        private static bool CreateGameDescriptionValidation() {
            return Selection.activeObject is SceneAsset;
        }
        
        [MenuItem("Assets/Create Game Description")]
        private static void CreateGameDescription() {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var name = Path.GetFileNameWithoutExtension(path);
            var sceneFileName = Path.GetFileName(path);
            var descPath = path.Replace(sceneFileName, name + "Description.asset");

            var description = CreateInstance<GameDescription>();
            description.ScenePath = path;
            ScriptableObjectUtility.CreateAssetAndFocus(description, descPath);

            var gListGuid = AssetDatabase.FindAssets("t:" + nameof(GamesList)).FirstOrDefault();
            if (gListGuid != null) {
                //insert description to games list
                var gamesList = AssetDatabase.LoadAssetAtPath<GamesList>(AssetDatabase.GUIDToAssetPath(gListGuid));
                var serialized = new SerializedObject(gamesList);
                var list = serialized.FindProperty("_games");
                var i = list.arraySize;
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = description;
                serialized.ApplyModifiedProperties();

                //insert scene to build if not
                if (EditorBuildSettings.scenes.All(s => s.path != path) && i > 0) {
                    var scenes = EditorBuildSettings.scenes.ToList();
                    var prevDesc = (GameDescription) list.GetArrayElementAtIndex(i - 1).objectReferenceValue;
                    i = scenes.FindIndex(s => s.path == prevDesc.ScenePath);
                    if (i >= 0) {
                        scenes.Insert( i + 1, new EditorBuildSettingsScene(path, true));
                        EditorBuildSettings.scenes = scenes.ToArray();
                    }
                }
                AssetDatabase.SaveAssets();
            } else {
                Debug.LogError(nameof(GamesList) + ".asset is not found!");
            }
        }
    }
}