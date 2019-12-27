using System.Collections;
using BezierSolution;
using Games.Common.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;
using Utilities;

namespace Games.Road {
    public class GameRoad : BaseGameWithGetDepth {
        [SerializeField] private CarController _car;
        [SerializeField] private Road _road;
        [SerializeField] private BezierSpline[] _splines;
        [SerializeField] private float _waitForEndGame = 0.5f;
        [SerializeField] private float _startShiftPos = 2f;

        private float _initialRoadWidth;
        private CarAIControl _carAI;
        private BezierWayPoint _wayPoint;
        
        protected override void Start() {
            _carAI = _car.GetComponent<CarAIControl>();
            _wayPoint = _car.GetComponent<BezierWayPoint>();
            SaveInitialSizes(_car);
            _initialRoadWidth = _road.width;
            _car.gameObject.SetActive(false);
            
            base.Start();
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

        private IEnumerator Driving(BezierSpline spline) {
            _road.SetPath(spline);
            _wayPoint.spline = spline;
            var t = 0f;
            _car.transform.position = spline.MoveAlongSpline(ref t, _startShiftPos * math.cmax(_car.transform.localScale));
            _car.transform.rotation = Quaternion.LookRotation(spline.GetTangent(t), _car.transform.up);
            _car.gameObject.SetActive(true);
            yield return null;
            _car.WakeUp();
            _carAI.Driving = true;
            yield return new WaitWhile(() => _carAI.Driving);
            yield return new WaitForSeconds(_waitForEndGame);
            GameEvent.Current = GameState.STOP;
        }

        protected override void StartGame() {
            base.StartGame();
            StartCoroutine(nameof(Driving), _splines.Random());
        }

        protected override void StopGame() {
            StopCoroutine(nameof(Driving));
            base.StopGame();
        }
    }
}