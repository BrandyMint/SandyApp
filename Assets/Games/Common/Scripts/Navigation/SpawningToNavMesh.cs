using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Common.Navigation {
    public class SpawningToNavMesh : MonoBehaviour {
        protected const int _ITERATIONS = 20;
        
        [Header("Spawn")]
        [SerializeField] protected Bounds _spawnBounds = new Bounds(Vector3.zero, Vector2.one * 0.8f);
        [SerializeField] protected float _maxSampleDistance = 0.2f;
        [SerializeField] protected float _timeNextIteration = 1f;
        
        public event Action OnSpawned;

        protected Transform _spawnField;
        protected bool _isSpawned;
        protected int _areaMask = NavMesh.AllAreas;
        
        public void RandomSpawn() {
            _isSpawned = false;
            StopCoroutine(nameof(Spawning));
            StartCoroutine(nameof(Spawning));
        }

        private IEnumerator Spawning() {
            while (true) {
                for (int i = 0; i < _ITERATIONS; ++i) {
                    var p = new Vector3(
                        Random.Range(_spawnBounds.min.x, _spawnBounds.max.x),
                        Random.Range(_spawnBounds.min.y, _spawnBounds.max.y),
                        Random.Range(_spawnBounds.min.z, _spawnBounds.max.z)
                    );
                    if (_spawnField != null) {
                        p = _spawnField.TransformPoint(p);
                    }
                    if (SamplePositionWithScaledDistance(p, out var hit, _maxSampleDistance)) {
                        transform.position = hit.position;
                        transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), transform.up);
                        _isSpawned = true;
                        OnSpawned?.Invoke();
                        yield break;
                    }
                }
                
                OnWaitSpawnNextIteration();
                yield return new WaitForSeconds(_timeNextIteration);
            }
        }

        protected virtual void OnWaitSpawnNextIteration() {
            transform.position = Vector3.one * 9999999f;
        }

        protected bool SamplePositionWithScaledDistance(Vector3 p, out NavMeshHit hit, float localDistance) {
            var sampleDist = localDistance;
            if (_spawnField != null) {
                var s = _spawnField.TransformVector(Vector3.one);
                sampleDist *= MathHelper.GetMedian(s.x, s.y, s.z);
            }

            return NavMesh.SamplePosition(p, out hit, sampleDist, _areaMask);
        }
    }
}