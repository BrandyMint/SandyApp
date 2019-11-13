using System;
using System.Collections;
using Games.Common;
using UnityEngine;

namespace Games.Balloons {
    [RequireComponent(typeof(RandomColorRenderer), typeof(ParticleSystem))]
    public class Balloon : MonoBehaviour {
        public static event Action<Balloon> OnDestroyed;
        public static event Action<Balloon, Collision> OnCollisionEntered;
        
        private ParticleSystem _particles;

        private void Awake() {
            _particles = GetComponent<ParticleSystem>();
        }

        private void OnDestroy() {
            OnDestroyed?.Invoke(this);
        }

        public void Bang() {
            gameObject.layer = 0;
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
            GetComponent<ConstantForce>().enabled = false;
            var body = GetComponent<Rigidbody>();
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            
            var module = _particles.main; 
            module.startColor = GetComponent<RandomColorRenderer>().GetColor();
            
            GetComponent<AudioSource>().Play();
            StartCoroutine(PlayParticlesAndDead());
        }

        private IEnumerator PlayParticlesAndDead() {
            _particles.Play();
            yield return new WaitForSeconds(_particles.main.duration);
            Dead();
        }

        public void Dead() {
            gameObject.layer = 0;
            Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision other) {
            OnCollisionEntered?.Invoke(this, other);
        }
    }
}