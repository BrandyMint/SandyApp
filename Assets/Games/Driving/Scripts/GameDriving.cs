using System.Collections;
using BezierSolution;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

namespace Games.Driving {
    public class GameDriving : BaseGameWithGetDepth {
        [SerializeField] private CarController _car;
        [SerializeField] private UICarControl _uiCarControl;
        [SerializeField] private bool _testMouse = false;

        protected override void Start() {
            _testMouseModeHold = true;
            
            SaveInitialSizes(_car);
            base.Start();
            _hitMask = LayerMask.GetMask("border");
            _car.gameObject.SetActive(false);
            _uiCarControl.Car = _car;
            _uiCarControl.OnResetCar += RespawnCar;
        }

        protected override void StartGame() {
            base.StartGame();
            _car.gameObject.SetActive(true);
            StartCoroutine(RespawnCarOnNextFrame());
        }

        private IEnumerator RespawnCarOnNextFrame() {
            yield return null;
            RespawnCar();
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            SetSizes(_gameField.Scale, _car);
            var carScale = math.cmax(_car.transform.localScale);
            _car.DoScale(carScale);
        }
        
        protected void FixedUpdate() {
            if (!_testMouse) return;
            _uiCarControl.StartDepthInput();
            base.Update();
            _uiCarControl.StopDepthInput();
        }

        protected override void ProcessDepthFrame() {
            _uiCarControl.StartDepthInput();
            base.ProcessDepthFrame();
            _uiCarControl.StopDepthInput();
        }

        protected override void Fire(Vector2 viewPos) {
            if (!_isGameStarted) return;
            _uiCarControl.DepthInput(viewPos);
        }

        public void RespawnCar() {
            var ray = _cam.ViewportPointToRay(Vector2.one / 2f);
            if (Physics.Raycast(ray, out var hit, _cam.farClipPlane, _hitMask)) {
                var p = hit.point;
                p.y += 2f* math.cmax(_car.transform.localScale);
                _car.transform.position = p;
                _car.transform.rotation = Quaternion.identity;
                _car.WakeUp();
            }
        }
    }
}