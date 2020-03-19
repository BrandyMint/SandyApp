using System.Collections;
using Games.Common;
using Games.Common.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Games.Landscape {
    [RequireComponent(typeof(NavMeshAgent))]
    public class AnimalWalkerNavMesh : SpawningToNavMesh, IAnimalWalker {
        [Header("Animal")]
        [SerializeField] protected float _speed = 0.2f;
        [SerializeField] protected float _minDistWalk = 0.1f;
        [SerializeField] protected float _maxDistWalk = 0.3f;
        [SerializeField] protected float _maxTurnOnStart = 90f;

        private NavMeshAgent _agent;
        private GameField _field;
        
        private bool _isInited;

        private void Awake() {
            _agent = GetComponent<NavMeshAgent>();
            OnSpawned += OnSpawnedSuccess;
        }

        public void Init(GameField field) {
            _field = field;
            _spawnField = field.transform;
            _isInited = true;
        }

        private void OnSpawnedSuccess() {
            _agent.enabled = true;
        }

        protected override void OnWaitSpawnNextIteration() {
            _agent.enabled = false;
            base.OnWaitSpawnNextIteration();
        }

        public IEnumerator WalkRandom() {
            var scale = math.cmax(transform.localScale);
            _agent.speed = _speed * scale;
            
            if (!_isInited || !_isSpawned)
                yield break;

            int i = 0;
            var currPos = transform.position;
            var destPos = currPos;
            var destPosValid = false; 
            do {
                if (i++ >= _ITERATIONS)
                    yield break;

                var dir = transform.rotation * Vector3.forward;
                if (i < _ITERATIONS / 2)
                    dir = Quaternion.AngleAxis(Random.Range(-0.5f, 0.5f) * _maxTurnOnStart, transform.up) * dir;
                else {
                    dir = Quaternion.AngleAxis(Random.Range(0f, 360f), transform.up) * dir;
                }
                destPos = currPos + Random.Range(_minDistWalk, _maxDistWalk) * _field.Scale * dir;
                destPosValid = NavMesh.SamplePosition(destPos, out var hit, SampleDist(), _areaMask);
                if (destPosValid)
                    destPos = hit.position;
            } while (!destPosValid);

            _agent.destination = destPos;
            _agent.isStopped = false;

            var wasGoodVelocity = false;
            while (true) {
                _agent.updateRotation = !_agent.pathPending;
                if (!_agent.pathPending) {
                    var velocity = _agent.velocity.magnitude;
                    var desiredVelocity = _agent.desiredVelocity.magnitude;
                    var velocityNotEmpty =  velocity > 0f && desiredVelocity > 0f;
                    var badVelocity = velocityNotEmpty && velocity / desiredVelocity < 0.8f;
                    wasGoodVelocity |= !badVelocity && velocityNotEmpty;
                    if (!wasGoodVelocity && badVelocity)
                        badVelocity = Vector3.Dot(_agent.velocity.normalized, _agent.desiredVelocity.normalized) < 0.6f;

                    if (_agent.remainingDistance < scale * 0.1f /*|| float.IsInfinity(_agent.remainingDistance)*/
                        || badVelocity && velocityNotEmpty) {
                        _agent.destination = transform.position;
                        _agent.isStopped = true;
                        yield break;
                    }
                }

                yield return null;
                
            }
        }

        public float CurrentAcceleration() {
            return _agent.velocity.magnitude / _agent.speed;
        }

        private float SampleDist() {
            return _field.Scale * 0.2f;
        }
    }
}