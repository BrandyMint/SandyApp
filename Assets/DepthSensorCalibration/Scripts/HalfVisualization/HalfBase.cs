using UnityEngine;

namespace DepthSensorCalibration.HalfVisualization {
    public abstract class HalfBase : MonoBehaviour, IHalfVisualization {
        [SerializeField] private FillType _fill;
        [SerializeField] private bool _hide;

        public FillType Fill {
            get => _fill;
            set {
                if (_fill != value) {
                    SetHalf(value);
                    _fill = value;
                }
            }
        }

        public bool Hide { get => _hide;
            set {
                if (_hide != value) {
                    _hide = value;
                    SetHalf(_fill);
                }
            }
        }

        private void Start() {
            SetHalf(_fill);
        }

        protected abstract void SetHalf(FillType type);


        protected virtual void OnValidate() {
            SetHalf(_fill);
        }
    }
}