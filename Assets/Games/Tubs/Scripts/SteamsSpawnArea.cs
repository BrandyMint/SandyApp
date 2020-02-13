using System.Linq;
using Games.Common;
using UnityEngine;
using Utilities;

namespace Games.Tubs {
    [RequireComponent(typeof(TubesGenerator))]
    public class SteamsSpawnArea : SpawnArea {
        private TubesGenerator _generator;

        protected override void Awake() {
            _generator = GetComponent<TubesGenerator>();
            _instances.Add(this);
        }

        public override bool GetRandomSpawn(out Vector3 worldPos, out Quaternion worldRot, 
            Vector3[] stayAway = null, float stayAwayDist = 1,
            Bounds[] stayAwayBounds = null) 
        {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            var tubes = _generator.Tubes.ToArray();
            if (!tubes.Any())
                return false;
            
            var iterations = tubes.Length;
            do {
                var s = tubes.Random();
                if (s.GetRandomSteamSpawn(out var p, out worldRot)
                && (StayAway(p, stayAway, stayAwayDist, stayAwayBounds))) {
                    worldPos = p;
                    return true;
                }
                --iterations;
            } while (iterations > 0);
            
            return false;
        }
    }
}