using UnityEngine;

namespace Games.Common {
    public class SpawnAreaCircle : SpawnArea {
        [SerializeField] private Transform _tplSpawn;
        [SerializeField] private float _radius = 0.5f;
        [SerializeField] private float _randomizePosition = 0.2f;
        [SerializeField] private int _count = 12;
        
        protected override void Awake() {
            GenerateSpawns();
            base.Awake();
        }

        private void GenerateSpawns() {
            var angle = 0f;
            var angleStep = Mathf.PI * 2f / _count;
            for (int i = 0; i < _count; ++i) {
                var spawn = i == 0 
                    ? _tplSpawn 
                    : Instantiate(_tplSpawn, _tplSpawn.transform.parent, false);
                var p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                spawn.localPosition = p * _radius / 2f;
                angle += angleStep;
            }
        }

        protected override Vector3 GetWorldPosition(Transform spawn) {
            var p = spawn.localPosition;
            var r = Random.Range(_radius - _randomizePosition, _radius + _randomizePosition) / 2f;
            p = p.normalized * r;
            return transform.TransformPoint(p);
        }
    }
}