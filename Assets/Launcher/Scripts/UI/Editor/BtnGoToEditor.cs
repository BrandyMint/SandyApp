using UnityEditor;

namespace Launcher.UI.Editor {
    [CustomEditor(typeof(BtnGoTo), true)]
    public class BtnGoToEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            var picker = target as BtnGoTo;
            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(serializedObject.FindProperty("_scenePath").stringValue);

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            var newScene = EditorGUILayout.ObjectField("scene", oldScene, typeof(SceneAsset), false) as SceneAsset;

            if (EditorGUI.EndChangeCheck()) {
                var newPath = AssetDatabase.GetAssetPath(newScene);
                var scenePathProperty = serializedObject.FindProperty("_scenePath");
                scenePathProperty.stringValue = newPath;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}