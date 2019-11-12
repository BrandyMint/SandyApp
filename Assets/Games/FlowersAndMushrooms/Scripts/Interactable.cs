using System;
using System.Collections;
using UnityEngine;

namespace Games.FlowersAndMushrooms {
    [RequireComponent(typeof(Renderer))]
    public class Interactable : MonoBehaviour {
        [SerializeField] private ParticleSystem _rightBang;
        [SerializeField] private ParticleSystem _wrongBang;

        public static event Action<Interactable> OnDestroyed;

        private Renderer _r;

        public int ItemType;

        private void Awake() {
            _r = GetComponent<Renderer>();
        }

        private void OnDestroy() {
            OnDestroyed?.Invoke(this);
        }

        public void Bang(bool isRight) {
            gameObject.layer = 0;
            _r.enabled = false;
            GetComponent<Collider>().enabled = false;
            
            StartCoroutine(PlayParticlesAndDead(isRight ? _rightBang : _wrongBang));
        }

        private IEnumerator PlayParticlesAndDead(ParticleSystem particles) {
            particles.Play();
            yield return new WaitForSeconds(particles.main.duration + particles.main.startLifetime.constant);
            Dead();
        }

        public void Dead() {
            gameObject.layer = 0;
            Destroy(gameObject);
        }
    }
}