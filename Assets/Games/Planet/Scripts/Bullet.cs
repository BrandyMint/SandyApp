using System;
using System.Collections.Generic;
using UnityEngine;

namespace Games.Planet {
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour {
        [SerializeField] public float speed = 1f;
        [SerializeField] private ParticleSystem _bang;
        [SerializeField] private float _maxLifeTime = 10f;
        [SerializeField] private AudioClip _audioFire;
        [SerializeField] private AudioClip _audioBang;
        [SerializeField] private GameObject _model;
        [SerializeField] private float _minAudioSimultaneouslyPlayShift = 0.2f;
        
        private AudioSource _audioSource; 

        public static event Action<Bullet, Collision> OnCollide;

        private static readonly Dictionary<int, float> _lastSoundPlayTimes = new Dictionary<int, float>();

        private void Awake() {
            _audioSource = GetComponent<AudioSource>();
            if  (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        private void Start() {
            GetComponent<Rigidbody>().velocity = speed * transform.forward;
            Destroy(gameObject, _maxLifeTime);
            PlayAudio(_audioFire);
        }

        private void OnCollisionEnter(Collision other) {
            OnCollide?.Invoke(this, other);
            
            _bang.transform.rotation = Quaternion.LookRotation(other.GetContact(0).normal);
            _bang.Play();
            PlayAudio(_audioBang);
            var destroyTime = Mathf.Max(_audioBang.length, _bang.main.duration + _bang.main.startLifetime.constant);
            _model.SetActive(false);
            
            Destroy(gameObject, destroyTime);
        }
        
        private void PlayAudio(AudioClip clip) {
            if (_audioSource != null && clip != null) {
                var clipId = clip.GetInstanceID();
                if (!_lastSoundPlayTimes.TryGetValue(clipId, out var lastTime)) {
                    lastTime = 0f;
                }
                if (Time.time - lastTime > _minAudioSimultaneouslyPlayShift) {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    _lastSoundPlayTimes[clipId] = Time.time;
                }
            }
        }
    }
}