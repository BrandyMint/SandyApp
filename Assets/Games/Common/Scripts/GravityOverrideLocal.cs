using UnityEngine;
using Utilities;

namespace Games.Common {
    public class GravityOverrideLocal : GravityOverride {
        [SerializeField] private bool _saveScale;

        private static Vector3 _defGravity;
        private static int _instances;

        protected override void Awake() {
            base.Awake();
            transform.SubscribeToUpdate(OnTransformUpdate);
        }

        protected override Vector3 GetNewGravity() {
            var g = transform.TransformVector(_gravity);
            if (_saveScale)
                return g.normalized * _gravity.magnitude; 
            return g;
        }

        private void OnTransformUpdate() {
            Physics.gravity = GetNewGravity();
        }

        protected override void OnDestroy() {
            transform.UnSubscribeFromUpdate(OnTransformUpdate);
            base.OnDestroy();
        }
    }
}