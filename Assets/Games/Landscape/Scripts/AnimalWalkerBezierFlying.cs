using UnityEngine;

namespace Games.Landscape {
    public class AnimalWalkerBezierFlying : AnimalWalkerBezier {
        [SerializeField] protected float _randomizeZFly = 0.5f;
        
        public float ZFly { get; set; }

        protected override Vector3 CorrectWalkEndPoint(Vector3 p) {
            p.z = ZFly * (1f + Random.Range(-_randomizeZFly, _randomizeZFly));
            return p;
        }
    }
}