using UnityEngine;

namespace Games.Common {
    public class GravityOverride : MonoBehaviour {
        [SerializeField] private Vector3 _gravity;

        private Vector3 _defGravity;

        private void Awake() {
            _defGravity = Physics.gravity;
            Physics.gravity = _gravity;
        }

        private void OnDestroy() {
            Physics.gravity = _defGravity;
        }
    }
}