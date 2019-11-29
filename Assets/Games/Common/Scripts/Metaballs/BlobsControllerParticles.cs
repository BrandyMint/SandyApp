using System;
using UnityEngine;

namespace Games.Common.Metaballs {
    [RequireComponent(typeof(ParticleSystem))] 
    public class BlobsControllerParticles : BlobsControllerBase {
        private ParticleSystem _particlesSystem;
        private ParticleSystem.Particle[] _particle = new ParticleSystem.Particle[1];

        private ParticleSystem ParticleSystem {
            get {
                if (_particlesSystem == null)
                    _particlesSystem = GetComponent<ParticleSystem>();
                return _particlesSystem;
            }
        }

        public override void GetBlob(int i, out Vector3 p, out float r) {
            ParticleSystem.GetParticles(_particle, 1, i);
            p = _particle[0].position;
            r = _particle[0].GetCurrentSize(ParticleSystem);
        }

        public override int BlobsCount() {
            return ParticleSystem.particleCount;
        }
    }
}