using System.Collections.Generic;
using System.Linq;
using DepthSensorSandbox.Visualisation;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Spray {
    public class SprayGame : BaseGameWithHandsRaycast {
        [SerializeField] private Spray[] _items;
        [SerializeField] private ProjectorDestination _projector;
        
        private HashSet<Spray> _fired = new HashSet<Spray>();
        private float _initialItemSize;
        private int _score;

        protected override void Start() {
            _initialItemSize = math.cmax(_items.First().transform.localScale);

            base.Start();
            ShowItems(false);
            _handsRaycaster.OnPostProcessDepthFrame += PostProcessFrame;
        }
        
        private void Spawn() {
            var i = 0;
            foreach (var area in SpawnArea.Areas) {
                foreach (var spawn in area.Spawns) {
                    var item = _items[i++];
                    item.transform.position = spawn.position;
                    item.transform.rotation = spawn.rotation;
                }
            }
        }
        
        private void PostProcessFrame() {
            CheckStopFire();
            _fired.Clear();
        }
        
        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
            var spray = (Spray) item;
            if (spray != null && !spray.Fire && !_fired.Contains(spray)) {
                spray.Fire = true;
                _fired.Add(spray);
            }
        }

        private void CheckStopFire() {
            foreach (var item in _items) {
                if (!_fired.Contains(item))
                    item.Fire = false;
            }
        }

        protected override void OnCalibrationChanged() {
            var cam = _cam.GetComponent<SandboxCamera>();
            var spawnArea = SpawnArea.Areas.First().transform;
            if (cam != null) {
                SetSizes(SelectDist(Prefs.Sandbox.ZeroDepth, Prefs.Sandbox.OffsetMaxDepth));
                CorrectSpraySpawns(spawnArea, Prefs.Sandbox.ZeroDepth + Prefs.Sandbox.OffsetMinDepth, _items.First());
            } else {
                SetSizes(SelectDist(1.66f, 0.35f)); //for testing
                CorrectSpraySpawns(spawnArea, 1.66f + 0.25f, _items.First());
            }
            
            Spawn();
        }

        private float SelectDist(float h, float offset) {
            _gameField.AlignToCamera(_cam, h - offset);
            var size = _gameField.Scale * _initialItemSize;
            return h - Mathf.Max(size * 1.5f, offset * 1.5f);
        }

        private void CorrectSpraySpawns(Transform spawnArea, float minH, Spray spray) {
            var plane = _gameField.PlaneOnDist(minH);
            var pUp = _gameField.PlaneRaycastFromViewport(plane, new Vector2(0.5f, 1f));
            var pUp2 = _gameField.PlaneRaycastFromViewport(plane, new Vector2(0.5f, 0f));
            if ((spawnArea.position - pUp2).sqrMagnitude > (spawnArea.position - pUp).sqrMagnitude)
                pUp = pUp2;
            var pDown = plane.ClosestPointOnPlane(spawnArea.position);
            var sprayAngle = spray.GetSprayAngle();
            var a = MathHelper.RightTriangleAngle(
                Vector3.Distance(pUp, pDown),
                Vector3.Distance(pDown, spawnArea.position)
            ) - sprayAngle / 2f;
            var spawnRot = spawnArea.localEulerAngles;
            spawnRot.x = 90f - a;
            spawnArea.localEulerAngles = spawnRot;
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            _gameField.SetWidth(0f);
            var size = _gameField.Scale * _initialItemSize;
            foreach (var item in _items) {
                item.transform.localScale = Vector3.one * size;
            }
        }
        
        private void ShowItems(bool show) {
            foreach (var item in _items) {
                item.Show(show);
            }
        }

        protected override void StartGame() {
            base.StartGame();
            ShowItems(true);
        }

        protected override void StopGame() {
            ShowItems(false);
            _fired.Clear();
            _projector.Clear();
            base.StopGame();
        }
    }
}