using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Launcher.MultiMonitorSupport {
    public class CameraVirtualTargetDisplayStore : MonoBehaviour {
        public int targetDisplay;
        public CameraClearFlags clearFlags;
        public float depth;
        
        private static readonly Dictionary<Camera, CameraVirtualTargetDisplayStore> _storeCache = 
            new Dictionary<Camera, CameraVirtualTargetDisplayStore>();

        private void OnDestroy() {
            foreach(var item in _storeCache.Where(kvp => kvp.Value == this).ToList()) {
                _storeCache.Remove(item.Key);
            }
        }

        public static bool CreateOrGet(Camera cam, out CameraVirtualTargetDisplayStore store) {
            store = cam.GetComponent<CameraVirtualTargetDisplayStore>();
            var isNew = store == null;
            if (isNew) {
                store = cam.gameObject.AddComponent<CameraVirtualTargetDisplayStore>();
                _storeCache.Add(cam, store);
            }
            return isNew;
        }

        public static int GetTargetDisplay(Camera cam) {
            if (_storeCache.TryGetValue(cam, out var store))
                return store.targetDisplay;
            return cam.targetDisplay;
        }

        public static CameraVirtualTargetDisplayStore Get(Camera cam) {
            if (_storeCache.TryGetValue(cam, out var store))
                return store;
            return null;
        }
    }
}