using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Balloons {
    [RequireComponent(typeof(Renderer), typeof(ParticleSystem))]
    public class Balloon : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        public static event Action<Balloon> OnDestroyed;
        public static event Action<Balloon, Collision> OnCollisionEntered;

        private Renderer _r;
        private ParticleSystem _particles;

        private void Awake() {
            _r = GetComponent<Renderer>();
            
            var startColor = _r.material.GetColor(_COLOR);
            Color.RGBToHSV(startColor, out _, out var s, out var v);
            startColor = Color.HSVToRGB(Random.value, s, v);

            
            var props = new MaterialPropertyBlock();
            _r.GetPropertyBlock(props);
            props.SetColor(_COLOR, startColor);
            _r.SetPropertyBlock(props);

            _particles = GetComponent<ParticleSystem>();
            var module = _particles.main; 
            module.startColor = startColor;
        }

        private void OnDestroy() {
            OnDestroyed?.Invoke(this);
        }

        public void Bang() {
            gameObject.layer = 0;
            _r.enabled = false;
            GetComponent<Collider>().enabled = false;
            GetComponent<ConstantForce>().enabled = false;
            var body = GetComponent<Rigidbody>();
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            
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