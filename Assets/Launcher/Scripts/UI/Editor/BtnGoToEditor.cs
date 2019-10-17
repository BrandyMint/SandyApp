using UnityEditor;

namespace Launcher.UI.Editor {
    [CustomEditor(typeof(BtnGoTo), true)]
    public class BtnGoToEditor : UnityEditor.Editor {
        SceneAssetInspector scenes = new SceneAssetInspector {
            sceneNames = {""}
        };
        
        public override void OnInspectorGUI() {
            scenes.OnInspectorGUI(serializedObject);
        }
    }
}