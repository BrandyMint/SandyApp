using System.Collections.Generic;
using Games.Balloons;
using UnityEngine;

namespace Games.Planet {
    public class Planet : MonoBehaviour {
        [SerializeField] private float _rotSpeed = 15f;
        [SerializeField] private Transform _planet;

        private Vector3 _initSize;
        private readonly List<Transform> _flyZones = new List<Transform>();

        private void Awake() {
            _initSize = transform.localScale;
            foreach (Transform child in transform) {
                if (child != _planet) {
                    child.gameObject.SetActive(false);
                    _flyZones.Add(child);
                }
            }
        }

        private void Update() {
            _planet.Rotate(Vector3.up, _rotSpeed * Time.deltaTime, Space.Self);
        }

        public void UpdateSize(GameField field) {
            transform.position = field.transform.position;
            var scale = Mathf.Min(field.transform.localScale.x, field.transform.localScale.y);
            transform.localScale = _initSize * scale;
        }

        public IReadOnlyList<Transform> FlyZones => _flyZones;
    }
}