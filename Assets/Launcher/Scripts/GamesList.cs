using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher {
    
    [CreateAssetMenu(fileName = "GamesList", menuName = "Custom Objects/Games List")]
    public class GamesList : ScriptableObject {
        private const string STORAGE_PATH = "GamesList";
        
        [SerializeField] private List<GameDescription> _games;

        private static List<GameDescription> _runtimeGamesDescriptions;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() {
            _runtimeGamesDescriptions = new List<GameDescription>();
            var list = Resources.Load<GamesList>(STORAGE_PATH);
            foreach (var game in list._games) {
                var i = SceneUtility.GetBuildIndexByScenePath(game.ScenePath);
                if (i >= 0) {
                    _runtimeGamesDescriptions.Add(game);
                } else {
                    Debug.LogError($"Cant find scene {game.ScenePath} in build");
                }
            }
        }

        public static int Count => _runtimeGamesDescriptions.Count;

        public static GameDescription GetDescription(int i) {
            return _runtimeGamesDescriptions[i];
        }

        public static GameDescription GetDescription(string scenePath) {
            return _runtimeGamesDescriptions.FirstOrDefault(d => d.ScenePath == scenePath);
        }
        
        public static GameDescription GetDescription(Scene scene) {
            return GetDescription(scene.path);
        }
        
        public static GameDescription GetDescriptionCurrent() {
            return GetDescription(SceneManager.GetActiveScene());
        }
        
        public static int GetId(string scenePath) {
            return _runtimeGamesDescriptions.FindIndex(d => d.ScenePath == scenePath);
        }
        
        public static int GetId(Scene scene) {
            return GetId(scene.path);
        }

        public static int GetIdCurrent() {
            return GetId(SceneManager.GetActiveScene());
        }

        public static bool IsGame(string scenePath) {
            return _runtimeGamesDescriptions.Any(d => d.ScenePath == scenePath);
        }
    }
}