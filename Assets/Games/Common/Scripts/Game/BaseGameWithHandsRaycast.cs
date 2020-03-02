using DepthSensorSandbox;
using DepthSensorSandbox.Visualisation;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Common.Game {
    public class BaseGameWithHandsRaycast : BaseGame {
        [SerializeField] private SandboxMesh _sandbox;

        protected int _hitMask;
        protected bool _testMouseModeHold;
        protected HandsRaycaster _handsRaycaster;

        protected override void Start() {
            if (DepthSensorSandboxProcessor.Instance != null)
                DepthSensorSandboxProcessor.Instance.HandsProcessingSwitch(true);
            _hitMask = LayerMask.GetMask("interactable");
            _handsRaycaster = CreateHandsRaycaster();
            SetCustomMaxHandDepth(0.04f);
            base.Start();
        }

        protected override void OnDestroy() {
            _handsRaycaster.SetEnable(false);
            base.OnDestroy();
        }

        protected void SetCustomMaxHandDepth(float d) {
            _handsRaycaster.MaxHandDepth = (ushort) (d * 1000f);
            var handsVisualizer = _sandbox.GetComponent<SandboxHands>();
            if (handsVisualizer != null)
                handsVisualizer.HandsDepthMax = d;
        }

        protected virtual HandsRaycaster CreateHandsRaycaster() {
            var raycaster = new HandsRaycaster {
                SandboxTransform = _sandbox.transform,
                Cam = _cam
            };
            raycaster.HandFire += Fire;
            return raycaster;
        }

        protected override void StartGame() {
            base.StartGame();
            _handsRaycaster.SetEnable(true);
        }

        protected override void StopGame() {
            _handsRaycaster.SetEnable(false);
            base.StopGame();
        }

        protected virtual void Update() {
            if (_testMouseModeHold ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0)) {
                _handsRaycaster.RaycastFromMouse();
            }
        }

        protected virtual void Fire(Ray ray, Vector2 uv) {
            if (!_isGameStarted) return;

            var cam = _handsRaycaster.Cam;
            //Debug.DrawRay(ray.origin, ray.direction);
            if (Physics.Raycast(ray, out var hit, cam.farClipPlane, _hitMask)) {
                var item = hit.collider.GetComponent<IInteractable>() ?? hit.collider.GetComponentInParent<IInteractable>();
                if (item != null) {
                    OnFireItem(item, uv);
                }
            }
        }

        protected virtual void OnFireItem(IInteractable item, Vector2 viewPos) {
        }
    }
}