using System;
using System.Collections;
using UnityEngine;

namespace Games.Common.GameFindObject {
    //[RequireComponent(typeof(Renderer))]
    public class Interactable : MonoBehaviour {
        [SerializeField] protected ParticleSystem _rightBang;
        [SerializeField] protected ParticleSystem _wrongBang;
        [SerializeField] protected int _itemType;
        [SerializeField] protected AudioClip _audioRight;
        [SerializeField] protected AudioClip _audioWrong;

        public static event Action<Interactable> OnDestroyed;

        protected Renderer _r;
        protected AudioSource _audioSource;

        public virtual int ItemType {
            get => _itemType;
            set => _itemType = value;
        }

        protected virtual void Awake() {
            _r = GetComponent<Renderer>();
            CreateAudioIfNeed();
        }

        private void OnDestroy() {
            OnDestroyed?.Invoke(this);
        }

        public virtual void Bang(bool isRight) {
            gameObject.layer = 0;
            _r.enabled = false;
            GetComponent<Collider>().enabled = false;
            StartCoroutine(PlayParticlesAndDead(isRight ? _rightBang : _wrongBang));
            PlayAudioBang(isRight);
        }
        
        public virtual void PlayAudioBang(bool isRight) {
            var clip = isRight ? _audioRight : _audioWrong;
            if (_audioSource != null && clip != null) {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
        }

        protected void CreateAudioIfNeed() {
            if (_audioRight != null || _audioWrong != null) {
                _audioSource = GetComponent<AudioSource>();
                if  (_audioSource == null)
                    _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        protected virtual IEnumerator PlayParticlesAndDead(ParticleSystem particles) {
            particles.Play();
            yield return new WaitForSeconds(particles.main.duration + particles.main.startLifetime.constant);
            Dead();
        }

        public virtual void Dead() {
            gameObject.layer = 0;
            Destroy(gameObject);
        }

        public virtual void Show(bool show) {
            _r.enabled = show;
        }
    }
}