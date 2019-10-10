using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher.Flip {
    public class CanvasFlipController : MonoBehaviour {
        private void Awake() {
            DontDestroyOnLoad(gameObject);
            AddCanvasFlippersFor(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += OnSceneLoaded;
            KeyMapper.AddListener(KeyEvent.FLIP_DISPLAY, OnFlipDisplay);
        }
        
        private void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            KeyMapper.RemoveListener(KeyEvent.FLIP_DISPLAY, OnFlipDisplay);
        }
        
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            AddCanvasFlippersFor(scene);
        }
        
        private static void AddCanvasFlippersFor(Scene scene) {
            foreach (var rootObj in scene.GetRootGameObjects()) {
                AddCanvasFlippersFor(rootObj);
            }
        }

        public static void AddCanvasFlippersFor(GameObject obj) {
            foreach (var canvas in obj.GetComponentsInChildren<Canvas>(true)) {
                if (canvas.GetComponent<TransformFlipper>() == null)
                    canvas.gameObject.AddComponent<TransformFlipper>();
            }
        }
        
        private static void OnFlipDisplay() {
            const int vertFlag = 1 << 1;
            const int horFlag = 1;
            int code = 0;
            if (Prefs.App.FlipVertical) code |= vertFlag;
            if (Prefs.App.FlipHorizontal) code |= horFlag;
            code = (code + 1) % 4;
            Prefs.App.FlipVertical = (code & vertFlag) == vertFlag;
            Prefs.App.FlipHorizontal = (code & horFlag) == horFlag;
            Prefs.App.Save();
        }
    }
}