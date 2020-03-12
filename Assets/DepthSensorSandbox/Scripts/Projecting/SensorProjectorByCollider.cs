using DepthSensorSandbox.Visualisation;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DepthSensorSandbox.Projecting {
    [RequireComponent(typeof(MeshCollider))]
    public class SensorProjectorByCollider : SensorProjector {
        private MeshCollider _collider;
        
        protected override void Awake() {
            _collider = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
            base.Awake();
        }

        public override Vector3 PlaceOnSandbox(Vector3 worldPos) {
            if (_collider == null)
                return worldPos;
            
            var b = _collider.bounds;
            var forward = transform.forward;
            var bCenter = Vector3.Project(b.center - transform.position, forward);
            var bSize = math.cmax(b.extents);
            var bTop = bCenter - forward * bSize;
            var origin = worldPos - Vector3.Project(worldPos - bTop, forward);
            var ray = new Ray(origin, forward);
            if (_collider.Raycast(ray, out var hit, bSize * 2f)) {
                return hit.point;
            } else {
                origin = worldPos + worldPos - origin;
                ray = new Ray(origin, -forward);
                if (_collider.Raycast(ray, out hit, bSize * 2f)) {
                    return hit.point;
                }
            }

            return worldPos;
        }
    }
}