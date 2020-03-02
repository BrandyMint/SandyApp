using System.Collections;
using System.Linq;
using BezierSolution;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Road {
    public class GameRoad : FindObjectGame {
        [SerializeField] private CarController _car;
        [SerializeField] private Road _road;
        [SerializeField] private BezierSpline[] _splines;
        [SerializeField] private float _waitForEndGame = 0.5f;
        [SerializeField] private float _startShiftPos = 2f;
        [SerializeField] private float _minSpawnDistance = 0.2f;
        [SerializeField] private float _maxSpawnDistance = 0.4f;

        private float _initialRoadWidth;
        private CarAIControl _carAI;
        private BezierWayPoint _wayPoint;
        private BezierSpline _currentSpline;

        protected override void Start() {
            _carAI = _car.GetComponent<CarAIControl>();
            _wayPoint = _car.GetComponent<BezierWayPoint>();
            SaveInitialSizes(_car);
            _initialRoadWidth = _road.width;
            _car.gameObject.SetActive(false);
            
            base.Start();
            Collidable.OnCollisionEntered += OnCollision;
        }

        protected override void OnDestroy() {
            Collidable.OnCollisionEntered -= OnCollision;
            base.OnDestroy();
        }

        private void OnCollision(Collidable collidable, Collision collision) {
            var item = collidable.GetComponentInParent<InteractableSimple>();
            if (item != null && item.CompareTag("Goal")) {
                if (collision.gameObject.CompareTag("Player")) {
                    item.hideOnBang = false;
                    item.destroyOnBang = false;
                    item.Bang(false);
                    _carAI.Driving = false;
                    GameScore.Lost = true;
                }
            }
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            _road.width = _initialRoadWidth *_gameField.Scale;
            _road.UpdateLines();
            SetSizes(_gameField.Scale, _car);
            var carScale = math.cmax(_car.transform.localScale);
            _car.DoScale(carScale);
            _carAI.DoScale(carScale);
        }

        private IEnumerator Driving() {
            _road.SetPath(_currentSpline);
            _wayPoint.spline = _currentSpline;
            var t = 0f;
            _car.transform.position = _currentSpline.MoveAlongSpline(ref t, _startShiftPos * math.cmax(_car.transform.localScale));
            _car.transform.rotation = Quaternion.LookRotation(_currentSpline.GetTangent(t), _car.transform.up);
            _car.gameObject.SetActive(true);
            yield return null;
            _car.WakeUp();
            _carAI.Driving = true;
            yield return new WaitWhile(() => _carAI.Driving);
            yield return new WaitForSeconds(_waitForEndGame);
            GameEvent.Current = GameState.STOP;
        }

        protected override void StartGame() {
            GameScore.Lost = false;
            _currentSpline = _splines.Random();
            _currentSpline.transform.localScale *= new float3(
                Random.value > 0.5f ? 1f : -1f,
                Random.value > 0.5f ? 1f : -1f,
                1f
            );
            base.StartGame();
            StartCoroutine(nameof(Driving));
        }

        protected override void StopGame() {
            StopCoroutine(nameof(Driving));
            base.StopGame();
        }

        protected override IEnumerator Spawning() {
            while (true) {
                if (_items.Count < _maxItems) {
                    SpawnItem(_tplItems.Random());
                }
                yield return new WaitForSeconds(_timeOffsetSpown);
            }
        }

        protected override InteractableSimple SpawnItem(InteractableSimple tpl) {
            if (_wayPoint.LastT + _minSpawnDistance * 1.2f > 1f)
                return null;
            
            var stayAway = _items.Select(b => b.transform.position).ToArray();
            var stayAwayDist = math.cmax(tpl.transform.localScale);
            var area = _currentSpline.GetComponent<SpawnAreaSpline>();
            area.minT = Mathf.Min(_wayPoint.LastT + _minSpawnDistance, 1f);
            area.maxT = Mathf.Min(_wayPoint.LastT + _maxSpawnDistance, 1f);
            if (area.GetRandomSpawn(out var worldPos, out var worldRot, stayAway, stayAwayDist)) {
                var newItem = Instantiate(tpl, worldPos, worldRot, tpl.transform.parent);
                newItem.gameObject.SetActive(true);
                _items.Add(newItem);
                
                return newItem;
            }

            return null;
        }

        protected override void OnFireItem(IInteractable item, Vector2 viewPos) {
            if (item.gameObject.CompareTag("Goal")) {
                ++GameScore.Score;
                item.Bang(true);
            }
        }
    }
}