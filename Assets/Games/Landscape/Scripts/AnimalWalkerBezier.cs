using System.Collections;
using BezierSolution;
using Games.Common;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Landscape {
    [RequireComponent(typeof(BezierWalkerWithSpeed))]
    public class AnimalWalkerBezier : MonoBehaviour, IAnimalWalker {
        protected const int _ITERATIONS = 40;
        
        [SerializeField] protected float _speed = 0.2f;
        [SerializeField] protected float _minDistWalk = 0.1f;
        [SerializeField] protected float _maxDistWalk = 0.3f;
        [SerializeField] protected float _maxAngleWalkTurn = 150f;
        [SerializeField] protected float _walkBorder = 0.5f;
        
        protected BezierSpline _spline;
        protected BezierWalkerWithSpeed _walker;
        protected bool _isInited;

        private void Awake() {
            _walker = GetComponent<BezierWalkerWithSpeed>();
            _spline = new GameObject(gameObject.name).AddComponent<BezierSpline>();
            _spline.Initialize(2);
            _spline.gameObject.SetActive(false);
            _spline.transform.SetParent(transform, false);
            _walker.onPathCompleted.AddListener(() => { _walker.enabled = false; });
            _walker.enabled = false;
        }
        
        private void OnDestroy() {
            if (_spline != null)
                Destroy(_spline.gameObject);
        }

        public void Init(GameField field) {
            _spline.transform.SetParent(field.transform, false);
            _isInited = true;
        }

        public void RandomSpawn() {
            var p = new Vector3(Random.value, Random.value) - Vector3.one / 2f;
            p *= 0.8f;
            CorrectWalkEndPoint(p);
            transform.position = _spline.transform.TransformPoint(p);
            transform.rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), transform.up);
        }

        public IEnumerator WalkRandom() {
            if (!_isInited)
                yield break;
            
            var start = _spline[0];
            start.position = transform.position;
            start.rotation = transform.rotation;

            Vector3 localEndPos;
            float dist;
            int i = 0;

            do {
                if (i++ >= _ITERATIONS)
                    yield break;

                dist = Random.Range(_minDistWalk, _maxDistWalk);
                var a = Random.Range(-_maxAngleWalkTurn, _maxAngleWalkTurn);
                var rot = Quaternion.AngleAxis(a, transform.up) * transform.rotation; 
                localEndPos = start.localPosition + rot * Vector3.forward * dist;
                localEndPos = CorrectWalkEndPoint(localEndPos);
            } while (Mathf.Abs(localEndPos.x) > _walkBorder || Mathf.Abs(localEndPos.y) > _walkBorder);
            
            start.followingControlPointLocalPosition = Vector3.forward * dist / 2f;

            var end = _spline[1];
            end.localPosition = localEndPos;
            var endDir = localEndPos - start.localPosition - start.localRotation * start.followingControlPointLocalPosition;
            end.localRotation = Quaternion.LookRotation(endDir, transform.up);
            end.precedingControlPointLocalPosition = Vector3.back * dist / 2f;
            
            _spline.gameObject.SetActive(true);
            _walker.spline = _spline;
            _walker.enabled = true;
            _walker.NormalizedT = 0f;
            while (_walker.enabled) {
                _walker.speed = _speed * math.cmax(transform.localScale);
                yield return null;
            }
            _spline.gameObject.SetActive(false);
        }

        public float CurrentAcceleration() {
            if (_walker.enabled)
                return 1f;
            return 0f;
        }

        protected virtual Vector3 CorrectWalkEndPoint(Vector3 p) {
            p.z = 0f;
            return p;
        }
    }
}