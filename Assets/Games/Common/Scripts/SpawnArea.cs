using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Common {
    public class SpawnArea : MonoBehaviour {
        [SerializeField] private Vector3 _randomizeAngle;

        public static IEnumerable<SpawnArea> Areas => _instances;

        private static readonly List<SpawnArea> _instances = new List<SpawnArea>();

        private readonly List<Transform> _spawns = new List<Transform>();

        protected virtual void Awake() {
            foreach (Transform child in transform) {
                child.gameObject.SetActive(false);
                _spawns.Add(child);
            }
            _instances.Add(this);
        }

        private void OnDestroy() {
            _instances.Remove(this);
        }

        public IEnumerable<Transform> Spawns => _spawns; 

        public static bool AnyGetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, Vector3[] stayAway = null, float stayAwayDist = 1f) {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            if (!_instances.Any())
                return false;
            
            for (int i = 0; i < _instances.Count; ++i) {
                var area = _instances.Random();
                if (area.GetRandomSpawn(out worldPos, out worldRot, stayAway, stayAwayDist))
                    return true;
            }

            return false;
        }
        
        public bool GetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, Vector3[] stayAway = null, float stayAwayDist = 1f) {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            if (!_spawns.Any())
                return false;
            
            var iterations = _spawns.Count;
            Transform spawn = null;
            do {
                var s = _spawns.Random();
                var p = worldPos = GetWorldPosition(s);
                if (stayAway == null || stayAway.All(a => Vector3.Distance(a, p) > stayAwayDist))
                    spawn = s;
                --iterations;
            } while (spawn == null && iterations > 0);

            if (spawn == null)
                return false;
            
            worldRot = GetWorldRotation(spawn);
            return true;
        }

        protected virtual Vector3 GetWorldPosition(Transform spawn) {
            return spawn.position;
        }

        protected virtual Quaternion GetWorldRotation(Transform spawn) {
            var noiseRotation = new Vector3(
                Random.Range(-_randomizeAngle.x, _randomizeAngle.x),
                Random.Range(-_randomizeAngle.y, _randomizeAngle.y),
                Random.Range(-_randomizeAngle.z, _randomizeAngle.z)
            );
            return spawn.rotation *  Quaternion.Euler(noiseRotation);
        }
    }
}