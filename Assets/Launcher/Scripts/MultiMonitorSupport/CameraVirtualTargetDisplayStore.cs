using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Launcher.MultiMonitorSupport {
    public class CameraVirtualTargetDisplayStore : MonoBehaviour {
        private int targetDisplay;
        
        private static readonly Dictionary<Camera, CameraVirtualTargetDisplayStore> _storeCache = 
            new Dictionary<Camera, CameraVirtualTargetDisplayStore>();

        private void OnDestroy() {
            foreach(var item in _storeCache.Where(kvp => kvp.Value == this).ToList()) {
                _storeCache.Remove(item.Key);
            }
        }

        public static void Store(Camera cam, int target) {
            var store = cam.GetComponent<CameraVirtualTargetDisplayStore>();
            if (store == null) {
                store = cam.gameObject.AddComponent<CameraVirtualTargetDisplayStore>();
                _storeCache.Add(cam, store);
            }

            store.targetDisplay = target;
        }

        public static int Get(Camera cam) {
            if (_storeCache.TryGetValue(cam, out var store))
                return store.targetDisplay;
            return cam.targetDisplay;
        }
    }
}