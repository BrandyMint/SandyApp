using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

namespace Games.Driving {
    public class UICarControl : MonoBehaviour {
        [SerializeField] private Camera _cam;
        [SerializeField] private RectTransform _Brake;
        [SerializeField] private RectTransform _Accel;
        [SerializeField] private RectTransform _Steering;
        [SerializeField] private RectTransform _Reset;
        [SerializeField] private RectTransform _SteeringInput;
        [SerializeField] private Image _AccelInput;
        [SerializeField] private Image _BrakeInput;


        public CarController Car {
            get { return _car; }
            set {
                _car = value;
                if (_car != null) {
                    _carControl = _car.GetComponent<CarUserControl>();
                } else {
                    _carControl = null;
                }
            }
        }

        public event Action OnResetCar;

        private float _maxAccel;
        private float _maxBrake;
        private float _maxSteer;
        private readonly List<float> _steeringsX = new List<float>();
        private bool _wasReset;

        private CarController _car;
        private CarUserControl _carControl;

        private void Update() {
            VisualizeInput();
        }


        private void VisualizeInput() {
            if (_car == null) return;
            
            var steerRotation = _SteeringInput.eulerAngles;
            steerRotation.z = -90f * _car.CurrentSteerAngle / _car.MaxSteerAngle;
            _SteeringInput.eulerAngles = steerRotation;

            _AccelInput.fillAmount = Mathf.Clamp01(_car.AccelInput);
            _BrakeInput.fillAmount = Mathf.Clamp01(_car.BrakeInput);
        }

        public void StartDepthInput() {
            _maxAccel = float.MinValue;
            _maxBrake = float.MinValue;
            _maxSteer = float.MinValue;
            _steeringsX.Clear();
            _wasReset = false;
        }

        public void DepthInput(Vector2 viewPos) {
            var screen = _cam.ViewportToScreenPoint(viewPos);

            if (Cast(_Brake, screen, out var p)) {
                _maxBrake = Mathf.Max(_maxBrake, p.y);
            }
            if (Cast(_Accel, screen, out p)) {
                _maxAccel = Mathf.Max(_maxAccel, p.y);
            }
            if (Cast(_Steering, screen, out p)) {
                _maxSteer = Mathf.Max(_maxSteer, p.y);
                _steeringsX.Add(p.x * 2 - 1f);
            }
            if (Cast(_Reset, screen, out p)) {
                _wasReset = true;
            }
        }
        
        

        public void StopDepthInput() {
            if (_maxAccel > 0f)
                _carControl.OverrideAccel(_maxAccel);
            else {
                _carControl.OverrideAccel(null);
            }
            if (_maxBrake > 0f)
                _carControl.OverrideFootbrake(-_maxBrake);
            else {
                _carControl.OverrideFootbrake(null);
            }
            if (_maxSteer > 0f) {
                var x = _steeringsX.Average();
                var y = _maxSteer - 0.3f;
                var s = -Vector2.SignedAngle(Vector2.up, new Vector2(x, y)) / 90f;
                _carControl.OverrideSteering(s);
            }else {
                _carControl.OverrideSteering(null);
            }
            if (_wasReset)
                OnResetCar?.Invoke();
        }

        public bool Cast(RectTransform t, Vector2 screen, out Vector2 local) {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(t, screen, _cam, out local)) {
                if (t.rect.Contains(local)) {
                    local = (local - t.rect.min) / t.rect.size;
                    return true;
                }
            }

            return false;
        }
    }
}