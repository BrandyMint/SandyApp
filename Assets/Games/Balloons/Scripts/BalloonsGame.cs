using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;

namespace Games.Balloons {
    public class BalloonsGame : BaseGameWithHandsRaycast {
        [SerializeField] private Balloon _tplBalloon;
        [SerializeField] private float _maxBallons = 12;
        [SerializeField] private float _timeOffsetSpown = 2f;
        [SerializeField] private float _startForce = 3f;
        [SerializeField] private float _explosionForceMult = 2f;
        [SerializeField] private float _explosionRadiusMult = 3f;

        private List<InteractableSimple> _balloons = new List<InteractableSimple>(); 
        private float _initialBallSize;
        private int _score;

        protected override void Start() {
            _initialBallSize = math.cmax(_tplBalloon.transform.localScale);
            _tplBalloon.gameObject.SetActive(false);

            base.Start();

            InteractableSimple.OnDestroyed += OnBalloonDestroyed;
            Balloon.OnCollisionEntered += OnBalloonCollisionEnter;
        }

        protected override void OnDestroy() {
            Balloon.OnCollisionEntered -= OnBalloonCollisionEnter;
            InteractableSimple.OnDestroyed -= OnBalloonDestroyed;
            base.OnDestroy();
        }

        private IEnumerator Spawning() {
            while (true) {
                if (_balloons.Count < _maxBallons) {
                    SpawnBalloon();
                }
                yield return new WaitForSeconds(_timeOffsetSpown);
            }
        }

        private void SpawnBalloon() {
            var stayAway = _balloons.Select(b => b.transform.position).ToArray();
            var size = math.cmax(_tplBalloon.transform.localScale);
            var stayAwayDist = size * 1.5f;
            var wSize = math.cmax(_tplBalloon.transform.lossyScale);
            var scaleMass = wSize;
            if (SpawnArea.AnyGetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var newBalloon = Instantiate(_tplBalloon, worldPos, worldRot, _tplBalloon.transform.parent);

                var rigid = newBalloon.GetComponent<Rigidbody>();
                newBalloon.gameObject.SetActive(true);

                
                newBalloon.FullMass = rigid.mass *= scaleMass;
                foreach (var strBodySegment in newBalloon.String.GetComponentsInChildren<Rigidbody>()) {
                    var m = strBodySegment.mass *= scaleMass;
                    newBalloon.FullMass += m;
                }
                
                var force = newBalloon.GetComponent<ConstantForce>();
                force.force = -Physics.gravity * newBalloon.FullMass
                    + _startForce * _gameField.Scale * newBalloon.FullMass * newBalloon.transform.forward;
                _balloons.Add(newBalloon);
            }
        }

        private void OnBalloonDestroyed(InteractableSimple balloon) {
            _balloons.Remove(balloon);
        }

        private void OnBalloonCollisionEnter(Balloon balloon, Collision collision) {
            var gameField = (BalloonsGameField) _gameField;
            if (gameField.ExitBorder.Contains(collision.collider)) {
                balloon.Dead();
            }
        }

        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
            var balloon = (Balloon) item;
            ++GameScore.Score;
            balloon.Bang(true);
            var force =  _explosionForceMult * _gameField.Scale * balloon.FullMass;
            var radius = math.cmax(balloon.transform.lossyScale) * _explosionRadiusMult;
            var pos = balloon.transform.position;
            foreach (var b in _balloons) {
                if (b != balloon) {
                    b.GetComponent<Rigidbody>().AddExplosionForce(force, pos, radius);
                }
            }
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var size = _gameField.Scale * _initialBallSize;
            _tplBalloon.transform.localScale = Vector3.one * size;
            _gameField.SetWidth(size * 2f);
        }

        private void ClearBalls() {
            foreach (var balloon in _balloons) {
                balloon.Dead();
            }
            _balloons.Clear();
        }

        protected override void StartGame() {
            ClearBalls();
            base.StartGame();
            StartCoroutine(nameof(Spawning));
        }

        protected override void StopGame() {
            StopCoroutine(nameof(Spawning));
            base.StopGame();
        }
    }
}