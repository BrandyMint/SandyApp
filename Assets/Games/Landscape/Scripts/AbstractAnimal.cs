using System.Collections;
using BezierSolution;
using Games.Common;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Landscape {
    public abstract class AbstractAnimal : MonoBehaviour {
        protected const int _ITERATIONS = 40;
        protected static readonly int _IDLE = Animator.StringToHash("idle");
        protected static readonly int _WALK = Animator.StringToHash("walk");
        protected static readonly int _EAT = Animator.StringToHash("eat");
        protected int[] _animStates;

        [SerializeField] protected float _speed = 0.2f;
        [SerializeField] protected float _minDistWalk = 0.1f;
        [SerializeField] protected float _maxDistWalk = 0.3f;
        [SerializeField] protected float _minTimeState = 1f;
        [SerializeField] protected float _maxTimeState = 3f;
        [SerializeField] protected float _maxAngleWalkTurn = 150f;
        [SerializeField] protected float _walkBorder = 0.5f;
        
        public GameField field { get; set; }

        protected Animator _anim;
        protected BezierSpline _spline;
        protected BezierWalkerWithSpeed _walker;

        private void Awake() {
            _anim = GetComponent<Animator>();
            _walker = GetComponent<BezierWalkerWithSpeed>();
            _spline = new GameObject(_anim.name).AddComponent<BezierSpline>();
            _spline.Initialize(2);
            _spline.gameObject.SetActive(false);
            _walker.onPathCompleted.AddListener(() => { _walker.enabled = false; });
            _walker.enabled = false;
            _animStates = GetAnimStates();
        }

        protected virtual int[] GetAnimStates() {
            return new[] {_IDLE, _WALK, _EAT};
        }

        private void OnDestroy() {
            if (_spline != null)
                Destroy(_spline.gameObject);
        }

        private void OnEnable() {
            StartAnimation();
        }

        public void StartAnimation() {
            if (field == null)
                return;
            _spline.transform.SetParent(field.transform, false);
            StartCoroutine(Living());
        }

        protected abstract IEnumerator Living();

        protected void SetAnimState(int state) {
            foreach (var s in _animStates) {
                if (s == state)
                    _anim.SetTrigger(s);
                else
                    _anim.ResetTrigger(s);
            }
        }
        
        protected IEnumerator WaitRandom() {
            yield return new WaitForSeconds(Random.Range(_minTimeState, _maxTimeState));
        }

        protected IEnumerator WalkRandom() {
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

        protected virtual Vector3 CorrectWalkEndPoint(Vector3 p) {
            p.z = 0f;
            return p;
        }
    }
}