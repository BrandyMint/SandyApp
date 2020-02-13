using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities;

namespace Launcher.UI {
    public class AvoidDeselectingByPointer : MonoBehaviour {
        [SerializeField] private ScrollRect _scrollView;
        
        private GameObject _lastSelected;
        
        private void Update() {
            if (EventSystem.current == null)
                return;
            var curr = EventSystem.current.currentSelectedGameObject;
            if (curr != null) {
                if (_lastSelected != curr && _scrollView != null) {
                    _scrollView.ScrollTo(curr.transform);
                }
                _lastSelected = curr;
            } else {
                EventSystem.current.SetSelectedGameObject(_lastSelected);
            }
        }
    }
}