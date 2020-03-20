using System.Collections;
using Games.Common;
using Games.Common.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Games.Landscape {
    [RequireComponent(typeof(NavMeshAgent))]
    public class AnimalWalkerNavMesh : MagnetToNavMeshAbstract, IAnimalWalker {
        [Header("Animal")]
        [SerializeField] protected float _speed = 0.2f;
        [SerializeField] protected float _minDistWalk = 0.1f;
        [SerializeField] protected float _maxDistWalk = 0.3f;
        [SerializeField] protected float _maxTurnOnStart = 90f;
        [SerializeField] protected float _lerpRotation = 0.1f;
        [SerializeField] protected float _lerpZWalk = 0.05f;

        private NavMeshAgent _agent;
        private GameField _field;
        
        private bool _isInited;
        private bool _isWalking;

        protected override void Awake() {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.updatePosition = false;
            _areaMask = _agent.areaMask;
            base.Awake();
        }

        public void Init(GameField field) {
            _field = field;
            _spawnField = field.transform;
            _isInited = true;
        }

        protected override void OnSpawnedSuccess() {
            _agent.enabled = true;
            base.OnSpawnedSuccess();
        }

        protected override void OnWaitSpawnNextIteration() {
            _agent.enabled = false;
            base.OnWaitSpawnNextIteration();
        }

        protected override void OnMagnetFail() {
            StartCoroutine(WalkRandom());
        }

        private bool FindRandomDestination(out Vector3 destPos) {
            int i = 0;
            var currPos = transform.position;
            destPos = currPos;
            var destPosValid = false;
            do {
                if (i++ >= _ITERATIONS) {
                    return false;
                }

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

            return true;
        }

        public IEnumerator WalkRandom() {
            var scale = math.cmax(transform.localScale);
            _agent.speed = _speed * scale;
            
            if (!_isInited || !_isSpawned || _isWalking || !FindRandomDestination(out var destPos))
                yield break;

            _agent.destination = destPos;
            _agent.isStopped = false;
            StopMagneting();
            _isWalking = true;

            var wasGoodVelocity = false;
            while (true) {
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
                        break;
                    }
                }

                var up = transform.up;
                var toNext = _agent.nextPosition - transform.position;
                var toNextZ = Vector3.Project(toNext, up);
                transform.position += toNext - toNextZ * (1f - _lerpZWalk);
                
                var dir = Vector3.ProjectOnPlane(_agent.velocity, up);
                var r1 = transform.rotation;
                var r2 = Quaternion.LookRotation(dir, up);
                transform.rotation = Quaternion.RotateTowards(
                    r1, 
                    Quaternion.Lerp(r1, r2, _lerpRotation),
                    _agent.angularSpeed * Time.deltaTime
                );

                yield return null;
            }
            _isWalking = false;
            StartMagneting();
        }

        public float CurrentAcceleration() {
            var horizontal = Vector3.ProjectOnPlane(_agent.velocity, transform.up);
            return horizontal.magnitude / _agent.speed;
        }

        private float SampleDist() {
            return _field.Scale * 0.2f;
        }
    }
}