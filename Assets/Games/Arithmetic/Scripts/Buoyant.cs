using UnityEngine;

namespace Games.Arithmetic {
    public class Buoyant : MonoBehaviour {
        public float waveHeight = 0.15f;
        public float waveFrequency = 0.5f;
        public float waveLength = 0.05f;

        private Vector3 _startPos;

        private void Awake() {
            _startPos = transform.localPosition;
        }

        private void Update() {
            transform.localPosition = _startPos + new Vector3(
                waveLength * Wave(_startPos.x), 
                waveLength * Wave(_startPos.y), 
                waveHeight * Wave(_startPos.z)
            );
        }

        private float Wave(float distance) {
            distance = (distance % waveLength) / waveLength;
            return Mathf.Sin(Time.time * Mathf.PI * 2.0f * waveFrequency
                                         + (Mathf.PI * 2.0f * distance));
        }
    }
}