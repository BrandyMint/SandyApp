using System;
using System.Collections;
using UnityEngine;

namespace Games.Common.GameFindObject {
    [RequireComponent(typeof(Renderer))]
    public class Interactable : MonoBehaviour {
        [SerializeField] protected ParticleSystem _rightBang;
        [SerializeField] protected ParticleSystem _wrongBang;

        public static event Action<Interactable> OnDestroyed;

        protected Renderer _r;

        public virtual int ItemType { get; set; }

        protected virtual void Awake() {
            _r = GetComponent<Renderer>();
        }

        private void OnDestroy() {
            OnDestroyed?.Invoke(this);
        }

        public virtual void Bang(bool isRight) {
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

        public virtual void Show(bool show) {
            _r.enabled = show;
        }
    }
}