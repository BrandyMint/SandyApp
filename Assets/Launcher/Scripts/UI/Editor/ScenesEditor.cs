using UnityEditor;

namespace Launcher.UI.Editor {
    [CustomEditor(typeof(Scenes), true)]
    public class ScenesEditor : UnityEditor.Editor {
        SceneAssetInspector scenes = new SceneAssetInspector {
            sceneNames = {
                "Main",
                //"ProjectorParams",
                "Calibration",
                "SandboxCalibration",
                "CalibrationView"
            }
        };
        
        public override void OnInspectorGUI() {
            scenes.OnInspectorGUI(serializedObject);
            DrawDefaultInspector ();
        }
    }
}