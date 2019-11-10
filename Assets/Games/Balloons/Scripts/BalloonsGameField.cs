﻿using System.Linq;
using UnityEngine;
using Utilities;

namespace Games.Balloons {
    public class BalloonsGameField : GameField {
        [SerializeField] private Collider _exitBorder;

        public Collider ExitBorder => _exitBorder;
        
        protected BorderInfo[] _spawns;

        protected override void Awake() {
            base.Awake();
            _spawns = transform.GetComponentsOnlyInChildren<SpawnArea>()
                .Select(c => new BorderInfo(c)).ToArray();
        }

        protected override void UpdateWidth() {
            base.UpdateWidth();
            var scaledWidth = GetScaledWidth();
            foreach (var spawn in _spawns) {
                var pos = scaledWidth * spawn.startPos / 2;
                spawn.transform.localPosition += (Vector3) pos;
                spawn.transform.localScale = spawn.startScale * (1 - scaledWidth);
            }
        }
    }
}