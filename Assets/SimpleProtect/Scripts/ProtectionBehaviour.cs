#if BUILD_PROTECT_COPY
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleProtect {
    public static class ProtectionBehaviour {
        private static readonly string _SCENE_PROTECTION_NAME = "NotUnlocked";
        private static bool _allow;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void BeforeSplashScreen() {
            _allow = Protection.ValidateKey(ProtectionStore.Load());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeFirstScene() {
            if (_allow || SceneManager.GetActiveScene().name == _SCENE_PROTECTION_NAME)
                return;
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneByBuildIndex(0));
            SceneManager.LoadScene(_SCENE_PROTECTION_NAME);
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene() {
            if (_allow)
                return;
            foreach (var gameObject in GetDontDestroyScene().GetRootGameObjects()) {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static Scene GetDontDestroyScene() {
            var obj = new GameObject("CatchDontDestroy");
            Object.DontDestroyOnLoad(obj);
            var scene = obj.scene;
            Object.DestroyImmediate(obj);
            return scene;
        }
    }
}
#endif