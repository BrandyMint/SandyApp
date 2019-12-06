using System;
using UnityEngine;

namespace Games.Planet {
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour {
        [SerializeField] public float speed = 1f;
        [SerializeField] private ParticleSystem _bang;
        [SerializeField] private float _maxLifeTime = 10f;

        public static event Action<Bullet, Collision> OnCollide;

        private void Start() {
            GetComponent<Rigidbody>().velocity = speed * transform.forward;
            Destroy(gameObject, _maxLifeTime);
        }

        private void OnCollisionEnter(Collision other) {
            OnCollide?.Invoke(this, other);
            
            _bang.transform.SetParent(transform.parent, true);
            _bang.transform.rotation = Quaternion.LookRotation(other.GetContact(0).normal);
            var module = _bang.main;
            module.stopAction = ParticleSystemStopAction.Destroy;
            _bang.Play();
            
            Destroy(gameObject);
        }
    }
}