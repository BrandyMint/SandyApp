using UnityEngine;

namespace Games.Common {
    public class GravityOverride : MonoBehaviour {
        [SerializeField] private Vector3 _gravity;

        private static Vector3 _defGravity;
        private static int _instances;

        private void Awake() {
            if (_instances <= 0)
                _defGravity = Physics.gravity;
            Physics.gravity = _gravity;
            ++_instances;
        }

        private void OnDestroy() {
            --_instances;
            if (_instances <= 0)
                Physics.gravity = _defGravity;
        }
    }
}