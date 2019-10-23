using UnityEngine;
using UnityEngine.EventSystems;

namespace Launcher.UI {
    public class AvoidDeselectingByPointer : MonoBehaviour {
        private GameObject _lastSelected;
        
        private void Update() {
            if (EventSystem.current == null)
                return;
            var curr = EventSystem.current.currentSelectedGameObject;
            if (curr != null) {
                _lastSelected = curr;
            } else {
                EventSystem.current.SetSelectedGameObject(_lastSelected);
            }
        }
    }
}