using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Launcher.UI {
    [RequireComponent(typeof(Button))]
    public class BtnGoTo : MonoBehaviour {
        [SerializeField] private string _scenePath;
        
        private void Awake() {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(OnBtn);
        }

        private void OnBtn() {
            SceneManager.LoadScene(_scenePath);
        }
    }
}