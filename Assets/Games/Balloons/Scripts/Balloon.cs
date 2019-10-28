using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Balloons {
    public class Balloon : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        public static event Action<Balloon> OnDestroyed;
        public static event Action<Balloon, Collision> OnCollisionEntered; 

        private void Awake() {
            var r = GetComponent<Renderer>();
            
            var startColor = r.material.GetColor(_COLOR);
            Color.RGBToHSV(startColor, out _, out var s, out var v);
            startColor = Color.HSVToRGB(Random.value, s, v);

            
            var props = new MaterialPropertyBlock();
            r.GetPropertyBlock(props);
            props.SetColor(_COLOR, startColor);
            r.SetPropertyBlock(props);
        }

        private void OnDestroy() {
            OnDestroyed?.Invoke(this);
        }

        public void Bang() {
            gameObject.layer = 0;
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