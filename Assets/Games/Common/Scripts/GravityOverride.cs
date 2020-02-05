using UnityEngine;

namespace Games.Common {
    public class GravityOverride : MonoBehaviour {
        [SerializeField] protected Vector3 _gravity;

        private static Vector3 _defGravity;
        private static int _instances;

        protected virtual void Awake() {
            if (_instances <= 0)
                _defGravity = Physics.gravity;
            Physics.gravity = GetNewGravity();
            ++_instances;
        }

        protected virtual Vector3 GetNewGravity() {
            return _gravity;
        }

        protected virtual void OnDestroy() {
            --_instances;
            if (_instances <= 0)
                Physics.gravity = _defGravity;
        }
    }
}