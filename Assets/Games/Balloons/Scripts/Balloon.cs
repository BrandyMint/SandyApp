using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Balloons {
    public class Balloon : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        public event Action<Balloon> OnDestroyed; 

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
            Destroy(gameObject);
        }
    }
}