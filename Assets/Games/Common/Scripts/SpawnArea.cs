﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Common {
    public class SpawnArea : MonoBehaviour {
        [SerializeField] private Vector3 _randomizeAngle;
        [SerializeField] private List<Transform> _spawns = new List<Transform>();

        public static IEnumerable<SpawnArea> Areas => _instances;

        protected static readonly List<SpawnArea> _instances = new List<SpawnArea>();

        protected virtual void Awake() {
            if (_spawns == null || !_spawns.Any()) {
                _spawns = new List<Transform>();
                foreach (Transform child in transform) {
                    if (child.gameObject.activeSelf)
                        _spawns.Add(child);
                }
            }
            foreach (var spawn in _spawns) {
                spawn.gameObject.SetActive(false);
            }
            _instances.Add(this);
        }

        private void OnDestroy() {
            _instances.Remove(this);
        }

        public IEnumerable<Transform> Spawns => _spawns;
        
        public static bool AnyGetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, Bounds[] stayAwayBounds) {
            return AnyGetRandomSpawn(out worldPos, out worldRot, null, 1f, stayAwayBounds);
        }

        public static bool AnyGetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot,
            Vector3[] stayAway = null, float stayAwayDist = 1f,
            Bounds[] stayAwayBounds = null) 
        {
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

        public virtual bool GetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, Bounds[] stayAwayBounds) {
            return GetRandomSpawn(out worldPos, out worldRot, null, 1f, stayAwayBounds);
        }
        
        public virtual bool GetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, 
            Vector3[] stayAway = null, float stayAwayDist = 1f,
            Bounds[] stayAwayBounds = null) 
        {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            if (!_spawns.Any())
                return false;
            
            var iterations = _spawns.Count;
            Transform spawn = null;
            do {
                var s = _spawns.Random();
                var p = worldPos = GetWorldPosition(s);
                if (StayAway(p, stayAway, stayAwayDist, stayAwayBounds))
                    spawn = s;
                --iterations;
            } while (spawn == null && iterations > 0);

            if (spawn == null) {
                foreach (var s in _spawns) {
                    var p = worldPos = GetWorldPosition(s);
                    if (StayAway(p, stayAway, stayAwayDist, stayAwayBounds)) {
                        spawn = s;
                        break;
                    }
                }
            }

            if (spawn == null)
                return false;
            
            worldRot = GetWorldRotation(spawn);
            return true;
        }

        protected bool StayAway(Vector3 p, Vector3[] stayAway, float stayAwayDist, Bounds[] stayAwayBounds) {
            return (stayAway == null || stayAway.All(a => Vector3.Distance(a, p) > stayAwayDist))
                   && (stayAwayBounds == null || stayAwayBounds.All(b => !b.Contains(p)));
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