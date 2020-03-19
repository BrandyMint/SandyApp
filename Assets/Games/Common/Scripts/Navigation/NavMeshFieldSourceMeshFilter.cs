using UnityEngine;
using UnityEngine.AI;

namespace Games.Common.Navigation {
    [RequireComponent(typeof(MeshFilter))]
    public class NavMeshFieldSourceMeshFilter : NavMeshFieldSource {
        private MeshFilter _filter;
        
        protected override void Awake() {
            _filter = GetComponent<MeshFilter>();
            base.Awake();
        }

        protected override bool GetBuildSource(out NavMeshBuildSource source) {
            var valid = _filter.sharedMesh != null;
            if (valid) {
                source = new NavMeshBuildSource {
                    shape = NavMeshBuildSourceShape.Mesh,
                    area = _area,
                    sourceObject = _filter.sharedMesh,
                    transform = transform.localToWorldMatrix
                };
                return true;
            } else {
                source = default;
                return false;
            }
        }
    }
}