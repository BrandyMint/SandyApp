using BezierSolution;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Planet {
    [RequireComponent(typeof(BezierWalker))]
    public class Ship : InteractableModel {
        private const int _ITERATIONS = 20;

        [SerializeField] private float _minPathDist = 0.5f;
        
        private BezierWalkerWithSpeed _walker;
        private BezierSpline _flySpline;
        
        public Transform FlyZone {
            get => _flySpline.transform;
            set {
                _flySpline = value.GetComponent<BezierSpline>();
                StartFlying();
            }
        }

        public Vector3 Spawn { get; private set; }

        protected override void Awake() {
            base.Awake();
            _walker = GetComponent<BezierWalkerWithSpeed>();
            _walker.onPathCompleted.AddListener(FlyNextPath);
        }

        private void Start() {
            Spawn = transform.position;
        }

        private void StartFlying() {
            _walker.speed = math.cmax(transform.lossyScale);
            _walker.spline = _flySpline;
            _flySpline.Initialize(2);
            FlyNextPath();
        }

        private void FlyNextPath() {
            var start = _flySpline[0];
            start.position = transform.position;
            start.rotation = transform.rotation;

            Vector3 localEndPos;
            var dist = 0f;
            int i = 0;

            do {
                if (i++ >= _ITERATIONS)
                    return;
                localEndPos = Random.onUnitSphere / 2f;
                dist = Vector3.Distance(localEndPos, start.localPosition);
            } while (dist < _minPathDist);
            
            start.followingControlPointLocalPosition = Vector3.forward * dist / 2f;

            var end = _flySpline[1];
            end.localPosition = localEndPos;
            //end.rotation = Quaternion.LookRotation(end.position - start.followingControlPointPosition, -transform.forward);
            var endDir = Vector3.ProjectOnPlane(localEndPos - start.localPosition, localEndPos);
            end.localRotation = quaternion.LookRotation(endDir, localEndPos);
            end.precedingControlPointLocalPosition = Vector3.back * dist / 2f;
        }
    }
}