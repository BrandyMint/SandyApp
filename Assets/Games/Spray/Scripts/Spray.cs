using UnityEngine;

namespace Games.Spray {
    public class Spray : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        
        [SerializeField] private Color _color = Color.red;
        [SerializeField] private float _projectAlpha = 0.3f;
        [SerializeField] private ParticleSystem _particle;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Collider _collider;
        [SerializeField] private Projector _projector;
        [SerializeField] private GameObject[] _objsToHide;
        
        private bool _fire;

        public bool Fire {
            get => _fire;
            set {
                if (_fire != value) {
                    _fire = value;
                    ChangeFire(value);
                }
            }
        }

        private void Awake() {
            var module = _particle.main;
            module.startColor = _color;

            _projector.fieldOfView = GetSprayAngle();
            _projector.material = new Material(_projector.material) {
                color = new Color(_color.r, _color.g, _color.b, _color.a * _projectAlpha)
            };
            
            ChangeFire(false);
        }

        public float GetSprayAngle() {
            var shape = _particle.shape;
            return shape.angle * 2.5f;
        }

        private void ChangeFire(bool fire) {
            _collider.enabled = !fire;
            _projector.enabled = fire;
            if (fire) {
                _particle.Play();
            } else {
                _particle.Stop();
            }
        }

        public void Show(bool show) {
            if (!show) Fire = false;

            foreach (var obj in _objsToHide) {
                obj.SetActive(show);
            }
        }
    }
}