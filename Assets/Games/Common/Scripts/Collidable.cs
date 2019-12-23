using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Games.Common {
    public class Collidable : MonoBehaviour {
        public static event Action<Collidable, Collision> OnCollisionEntered;
        public static event Action<Collidable, Collision2D> OnCollisionEntered2D;

        public static readonly Vector2[] Dirs9 = GetDirs9().ToArray();

        private static IEnumerable<Vector2> GetDirs9() {
            for (int x = -1; x <= 1; ++x) {
                for (int y = -1; y <= 1; ++y) {
                    if (x != 0 || y != 0)
                        yield return new Vector2(x, y).normalized;
                }
            }
        }

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
        
        public void MakeAbsoluteBounceClampNormalsFor9Dirs(Collision2D collision) {
            MakeAbsoluteBounce(collision, Dirs9);
        }

        public void MakeAbsoluteBounce(Collision2D collision, IEnumerable<Vector2> clampNormals = null) {
            var normal = Vector2.zero;
            foreach (var contact in collision.contacts) {
                normal += contact.normal / collision.contacts.Length;
            }
            if (clampNormals != null) {
                var closestDot = float.MinValue;
                var closetNormal = normal;
                foreach (var clampNormal in clampNormals) {
                    var dot = Vector2.Dot(clampNormal, normal);
                    if (dot > closestDot) {
                        closestDot = dot;
                        closetNormal = clampNormal;
                    }
                }

                normal = closetNormal;
            }
            var velocity = LastFrameVelocity;
            if (Vector2.Dot(velocity, normal) <= 0f)
                LastFrameVelocity = Velocity = Vector3.Reflect(velocity, normal);
            else {
                LastFrameVelocity = Velocity;
            }
        }
        
        public void MakeAbsoluteBounce(Collision collision) {
            var normal = Vector3.zero;
            foreach (var contact in collision.contacts) {
                normal += contact.normal / collision.contacts.Length;
            }
            var velocity = LastFrameVelocity;
            if (Vector3.Dot(velocity, normal) <= 0f)
                LastFrameVelocity = Velocity = Vector3.Reflect(velocity, normal);
            else {
                LastFrameVelocity = Velocity;
            }
        }
    }
}