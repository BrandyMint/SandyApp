using UnityEngine;

namespace Games.Common {
    public class SpawnAreaSquare : SpawnAreaCircle {
        protected override void GenerateSpawns() {
            var angle = 0f;
            var angleStep = Mathf.PI * 2f / _count;
            for (int i = 0; i < _count; ++i) {
                var spawn = i == 0 
                    ? _tplSpawn 
                    : Instantiate(_tplSpawn, _tplSpawn.transform.parent, false);
                var p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var scale = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                spawn.localPosition = _radius / scale / 2f * p;
                angle += angleStep;
            }
        }
    }
}