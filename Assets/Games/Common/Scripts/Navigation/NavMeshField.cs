using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Games.Common.Navigation {
    [RequireComponent(typeof(GameField))]
    public class NavMeshField : MonoBehaviour {
        public float MinDepth = 1.6f;
        public float MaxDepth = 1.52f;
        public float AgentRadius = 0.01f;
        [Header("Agent Settings")]
        [HideInInspector] public int agentTypeID;
        public float AgentSlope = 45f;
        public float RelativeMinRegionArea = 0.1f;
        public int VoxelsInHeight = 100;
        public int TileSize = 128;
        
        private GameField _field;
        
        private NavMeshData _navMesh;
        private AsyncOperation _navMeshUpdatingOp;
        private NavMeshDataInstance _navMeshInstance;
        private List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
        
        private NavMeshBuildSettings _settings;

        private void Awake() {
            _settings = NavMesh.GetSettingsByID(0);
            _field = GetComponent<GameField>();
            
            _settings.overrideTileSize = true;
            _settings.overrideVoxelSize = true;
            UpdateSettings();
        }

        void OnEnable() {
            _navMesh = new NavMeshData();
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            UpdateNavMesh();
        }

        void OnDisable() {
            _navMeshInstance.Remove();
        }

        private IEnumerator Start() {
            while (true) {
                UpdateNavMesh();
                yield return _navMeshUpdatingOp;
            }
        }
        
        void UpdateNavMesh() {
            UpdateSettings();
            NavMeshFieldSource.Collect(ref _sources);
            var bounds = GetWorldBounds(MinDepth, MinDepth);
            _navMeshUpdatingOp = NavMeshBuilder.UpdateNavMeshDataAsync(_navMesh, _settings, _sources, bounds);
        }

        private void UpdateSettings() {
            _settings.agentTypeID = agentTypeID;
            _settings.agentRadius = AgentRadius;
            _settings.agentHeight = Mathf.Abs(MaxDepth - MinDepth);
            _settings.agentSlope = AgentSlope;
            _settings.agentClimb = _settings.agentHeight / 2f;
            _settings.minRegionArea = _field.Scale * RelativeMinRegionArea;
            _settings.voxelSize = _field.Scale / VoxelsInHeight;
            _settings.tileSize = TileSize;
        }

        /*private Bounds GetLocalBounds() {
            var size = math.abs(_field.transform.localScale);
            var pos = transform.InverseTransformPoint(_field.CenterPosOnDist(MinDepth) + _field.CenterPosOnDist(MaxDepth)) / 2f;
            pos.x = pos.y = 0f;
            pos.z *= size.z;
            size.z = Mathf.Abs(MinDepth - MaxDepth);
            return new Bounds(pos, size);
        }*/
        
        private Bounds GetWorldBounds(float depthMin, float depthMax) {
            var localMin = transform.InverseTransformPoint(_field.CenterPosOnDist(depthMin));
            localMin.x = localMin.y = -0.5f;
            var localMax = transform.InverseTransformPoint(_field.CenterPosOnDist(depthMax));
            localMax.x = localMax.y = 0.5f;
            var min = transform.TransformPoint(localMin);
            var max = transform.TransformPoint(localMax);
            var center = (min + max) / 2f;
            var size = math.abs(max - center) * 2;
            return new Bounds(center, size);
        }
        
        private Bounds GetWorldBounds() {
            return GetWorldBounds(MinDepth, MaxDepth);
        }

        private void OnDrawGizmosSelected() {
            if (_navMesh) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_navMesh.sourceBounds.center, _navMesh.sourceBounds.size);
            }

            if (_field != null) {
                Gizmos.color = Color.yellow;
                var bounds = GetWorldBounds();
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}