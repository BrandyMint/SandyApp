using UnityEngine;

namespace Games.Common {
    public class SpawnAreaLine : SpawnArea {
        [SerializeField] protected Transform _tplSpawn;
        [SerializeField] private float _randomizePosition = 0.2f;
        [SerializeField] protected int _count = 12;
        
        protected override void Awake() {
            GenerateSpawns();
            base.Awake();
        }

        protected virtual void GenerateSpawns() {
            if (_count < 2)  return;
            var offset = 1f / (_count - 1);
            for (int i = 0; i < _count; ++i) {
                var spawn = i == 0 
                    ? _tplSpawn 
                    : Instantiate(_tplSpawn, _tplSpawn.transform.parent, false);
                var p = new Vector2(i * offset, 0) - Vector2.right / 2f;
                spawn.localPosition = p;
            }
        }

        protected override Vector3 GetWorldPosition(Transform spawn) {
            var p = spawn.localPosition;
            var r = new Vector3(RandomizePos(), RandomizePos(), 0f);
            p += r;
            return transform.TransformPoint(p);
        }

        private float RandomizePos() {
            return Random.Range(-_randomizePosition, _randomizePosition) / 2f;
        }
    }
}