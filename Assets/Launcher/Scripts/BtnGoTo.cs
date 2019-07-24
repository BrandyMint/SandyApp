using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Launcher {
    [RequireComponent(typeof(Button))]
    public class BtnGoTo : MonoBehaviour {
        [SerializeField] private Object _scene;
        
        private void Awake() {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(OnBtn);
        }

        private void OnBtn() {
            var scenePath = AssetDatabase.GetAssetPath(_scene);
            SceneManager.LoadScene(scenePath);
        }
    }
}