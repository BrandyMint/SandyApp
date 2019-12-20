using System;
using UnityEngine;

namespace Games.Common {
    public class Collidable : MonoBehaviour {
        public static event Action<Collidable, Collision> OnCollisionEntered;
        public static event Action<Collidable, Collision2D> OnCollisionEntered2D;
        
        public Vector3 LastFrameVelocity { get; private set; }

        private void OnCollisionEnter(Collision other) {
            OnCollisionEntered?.Invoke(this, other);
        }
        
        private void OnCollisionEnter2D(Collision2D other) {
            OnCollisionEntered2D?.Invoke(this, other);
        }

        public void Stop() {
            var rigid = GetComponent<Rigidbody>();
            if (rigid != null) {
                rigid.velocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
            } else {
                var rigid2d = GetComponent<Rigidbody2D>();
                if (rigid2d != null) {
                    rigid2d.velocity = Vector2.zero;
                    rigid2d.angularVelocity = 0f;
                }
            }
        }
        
        private void Update() {
            LastFrameVelocity = Velocity;
        }

        public Vector3 Velocity {
            get {
                var rigid = GetComponent<Rigidbody>();
                if (rigid != null) {
                    return rigid.velocity;
                } else {
                    var rigid2d = GetComponent<Rigidbody2D>();
                    if (rigid2d != null) {
                        return rigid2d.velocity;
                    }
                }
                return Vector3.zero;
            }
            set {
                var rigid = GetComponent<Rigidbody>();
                if (rigid != null) {
                    rigid.velocity = value;
                } else {
                    var rigid2d = GetComponent<Rigidbody2D>();
                    if (rigid2d != null) {
                        rigid2d.velocity = value;
                    }
                }
            }
        }

        public void MakeAbsoluteBounce(Collision2D collision) {
            var normal = Vector2.zero;
            var point = Vector2.zero;
            foreach (var contact in collision.contacts) {
                normal += contact.normal / collision.contacts.Length;
                point += contact.point / collision.contacts.Length;
            }
            var velocity = LastFrameVelocity;
            var dir = point - (Vector2) transform.position;
            if (Vector2.Dot(velocity, dir) >= 0f)
                Velocity = Vector3.Reflect(velocity, normal);
        }
        
        public void MakeAbsoluteBounce(Collision collision) {
            var normal = Vector3.zero;
            var point = Vector3.zero;
            foreach (var contact in collision.contacts) {
                normal += contact.normal / collision.contacts.Length;
                point += contact.point / collision.contacts.Length;
            }
            var velocity = LastFrameVelocity;
            var dir = point - transform.position;
            if (Vector3.Dot(velocity, dir) >= 0f)
                Velocity = Vector3.Reflect(velocity, normal);
        }
    }
}