using Unity.Mathematics;
using UnityEngine;

namespace Games.Common {
    [RequireComponent(typeof(TrailRenderer))]
    public class TrailSize : MonoBehaviour {
        private TrailRenderer _trail;

        private void Start() {
            _trail = GetComponent<TrailRenderer>();
        }

        private void FixedUpdate() {
            _trail.widthMultiplier = math.length(math.abs(transform.TransformVector(Vector3.one))) / 2f;
        }
    }
}