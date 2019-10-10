using UnityEngine;
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
            Scenes.GoToWithCheckCalibration(_scenePath);
        }
    }
}