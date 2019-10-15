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
            KeyMapper.AddListener(KeyEvent.FLIP_SANDBOX, OnFlipSandbox);
        }
        
        private void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            KeyMapper.RemoveListener(KeyEvent.FLIP_DISPLAY, OnFlipDisplay);
            KeyMapper.RemoveListener(KeyEvent.FLIP_SANDBOX, OnFlipSandbox);
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

        private static void Flip(ref bool vertical, ref bool horizontal) {
            const int vertFlag = 1 << 1;
            const int horFlag = 1;
            int code = 0;
            if (vertical) code |= vertFlag;
            if (horizontal) code |= horFlag;
            code = (code + 1) % 4;
            vertical = (code & vertFlag) == vertFlag;
            horizontal = (code & horFlag) == horFlag;
            Prefs.App.Save();
        }

        private static void OnFlipDisplay() {
            var horizontal = Prefs.App.FlipHorizontal;
            var vertical = Prefs.App.FlipVertical;
            Flip(ref vertical, ref horizontal);
            Prefs.App.FlipHorizontal = horizontal;
            Prefs.App.FlipVertical = vertical;
        }

        private static void OnFlipSandbox() {
            var horizontal = Prefs.App.FlipHorizontalSandbox;
            var vertical = Prefs.App.FlipVerticalSandbox;
            Flip(ref vertical, ref horizontal);
            Prefs.App.FlipHorizontalSandbox = horizontal;
            Prefs.App.FlipVerticalSandbox = vertical;
        }
    }
}