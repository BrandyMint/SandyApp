using UnityEngine;

namespace Games.Common {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class LightRangeScale : MonoBehaviour {
        public float _localRange = 10f;

        private Light _light;

        private void Awake() {
            _light = GetComponent<Light>();
        }
        
#if UNITY_EDITOR
        private void OnEnable() {
            Awake();
        }
#endif

#if UNITY_EDITOR
        private void LateUpdate() {
            if (!UnityEditor.EditorApplication.isPlaying)
                FixedUpdate();
        }
#endif

        private void FixedUpdate() {
            var range = transform.TransformVector(Vector3.forward * _localRange).magnitude;
            _light.range = range;
        }
    }
}