using System.Linq;
using Games.Common;
using UnityEngine;
using Utilities;

namespace Games.Balloons {
    public class BalloonsGameField : GameField {
        [SerializeField] private Collider[] _exitBorder;

        public Collider[] ExitBorder => _exitBorder;
        
        protected BorderInfo[] _spawns;

        protected override void Awake() {
            base.Awake();
            _spawns = _bordersRoot.GetComponentsOnlyInChildren<SpawnArea>()
                .Select(c => new BorderInfo(c)).ToArray();
        }

        protected override void UpdateWidth() {
            base.UpdateWidth();
            var scaledWidth = GetScaledWidth();
            foreach (var spawn in _spawns) {
                var pos = scaledWidth * spawn.startPos;
                spawn.transform.localPosition = spawn.startPos + pos * 1.5f;
                spawn.transform.localScale = spawn.startScale * (1 - scaledWidth);
            }

            foreach (var t in _offsetedByWidth) {
                var s = t.localPosition;
                s += (Vector3)(s.normalized * scaledWidth / 2f);
                t.localPosition = s;
            }
        }
    }
}