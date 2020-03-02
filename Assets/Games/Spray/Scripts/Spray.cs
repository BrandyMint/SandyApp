using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Spray {
    public class Spray : MonoBehaviour, IInteractable {
        [SerializeField] private Color _color = Color.red;
        [SerializeField] private float _projectAlpha = 0.3f;
        [SerializeField] private ParticleSystem _particle;
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

        public int ItemType { get; set; }

        public void Bang(bool isRight) {
            Fire = isRight;
        }

        public void PlayAudioBang(bool isRight) {
            throw new System.NotImplementedException();
        }

        public void Dead() {
            gameObject.layer = 0;
            Destroy(gameObject);
        }

        public void Show(bool show) {
            if (!show) Fire = false;

            foreach (var obj in _objsToHide) {
                obj.SetActive(show);
            }
        }
    }
}