using UnityEngine;

namespace Games.Common {
    public class FaceToCamera : MonoBehaviour {
        [SerializeField] private Camera _cam;

        private void Start() {
            if (_cam == null)
                _cam = Camera.main;
        }

        private void Update() {
            transform.rotation = Quaternion.LookRotation(_cam.transform.position - transform.position, _cam.transform.up);
        }
    }
}