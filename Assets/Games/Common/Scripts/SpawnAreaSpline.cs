using BezierSolution;
using UnityEngine;

namespace Games.Common {
    [RequireComponent(typeof(BezierSpline))]
    public class SpawnAreaSpline : SpawnArea {
        [SerializeField] protected Transform _tplSpawn;
        [SerializeField] private float _randomizePosition = 0.2f;
        public float minT = 0f;
        public float maxT = 1f;
        
        
        private BezierSpline _spline;
        private Quaternion _initialRot;
        

        protected override void Awake() {
            _tplSpawn.gameObject.SetActive(false);
            _initialRot = _tplSpawn.rotation;
            _spline = GetComponent<BezierSpline>();
            _instances.Add(this);
        }

        public override bool GetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, 
            Vector3[] stayAway = null, float stayAwayDist = 1,
            Bounds[] stayAwayBounds = null) 
        {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            
            var iterations = 20;
            do {
                var t = Random.Range(minT, maxT);
                _tplSpawn.position = _spline.GetPoint(t);
                var p = GetWorldPosition(_tplSpawn);
                if (StayAway(p, stayAway, stayAwayDist, stayAwayBounds)) {
                    worldPos = p;
                    var rot = Quaternion.LookRotation(_spline.GetTangent(t), -transform.forward);
                    _tplSpawn.rotation = _initialRot * rot;
                    worldRot = GetWorldRotation(_tplSpawn);
                    return true;
                }
                --iterations;
            } while (iterations > 0);
            
            return false;
        }

        protected override Vector3 GetWorldPosition(Transform spawn) {
            var p = spawn.localPosition;
            var d = new Vector3(
                Random.Range(-_randomizePosition, _randomizePosition),
                Random.Range(-_randomizePosition, _randomizePosition)
            ) / 2f;
            var r = 
            p += d;
            return transform.TransformPoint(p);
        }
    }
}