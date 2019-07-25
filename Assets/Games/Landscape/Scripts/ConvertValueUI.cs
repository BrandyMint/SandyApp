using UnityEngine;

namespace Games.Landscape {
    public class ConvertValueUI : MonoBehaviour {
        [SerializeField] private float _convertMultiply = 1f;

        public float Get(float v) {
            return v / _convertMultiply;
        }

        public float Set(float v) {
            return v * _convertMultiply;
        }
    }
}