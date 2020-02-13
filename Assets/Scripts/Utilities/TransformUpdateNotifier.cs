using UnityEngine;
using UnityEngine.Events;

namespace Utilities {
    [DisallowMultipleComponent]
    public class TransformUpdateNotifier : MonoBehaviour {
        public UnityEvent OnTransformUpdated = new UnityEvent();
        
        private void LateUpdate() {
            if (transform.hasChanged && OnTransformUpdated != null) {
                transform.hasChanged = false;
                OnTransformUpdated.Invoke();
            }
        }
    }

    public static class TransformUpdateNotifierTools {
        public static void SubscribeToUpdate(this Transform tr, UnityAction action, bool execAtOnce = false) {
            var notifier = tr.GetComponent<TransformUpdateNotifier>()
                           ?? tr.gameObject.AddComponent<TransformUpdateNotifier>();
            if (action != null) {
                notifier.OnTransformUpdated.AddListener(action);
                if (execAtOnce) {
                    action.Invoke();
                }
            }
        }
        
        public static void UnSubscribeFromUpdate(this Transform tr, UnityAction action) {
            var notifier = tr.gameObject.GetComponent<TransformUpdateNotifier>();
            if (notifier == null)
                return;
            if (action != null) {
                notifier.OnTransformUpdated.RemoveListener(action);
            }
        }
    }
}