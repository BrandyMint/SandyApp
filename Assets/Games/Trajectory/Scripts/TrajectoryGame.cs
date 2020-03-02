using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Games.Trajectory {
    public class TrajectoryGame : BaseGame {
        [SerializeField] private InteractableSimple _ballTpl;
        [SerializeField] private Transform _goal;
        [SerializeField] private SpawnArea[] _ballSpawns;
        [SerializeField] private SpawnArea _goalSpawns;
        [SerializeField] private float _ballSpawnOffset = 1.5f;
        [SerializeField] private float _ballStartVelocity = 10f;
        [SerializeField] private float _waitBeforeWin = 0.5f;
        [SerializeField] private float _areaForNoGoals = 0.4f;
        
        private readonly List<InteractableSimple> _balls = new List<InteractableSimple>();
        private readonly Dictionary<SpawnArea, float> _initialSpawnZ = new Dictionary<SpawnArea, float>();

        protected override void Start() {
            foreach (var ballSpawn in _ballSpawns) {
                _initialSpawnZ.Add(ballSpawn, ballSpawn.transform.localPosition.z);
            }
            SaveInitialSizes(_ballTpl, _goal);
            _ballTpl.gameObject.SetActive(false);
            _goal.gameObject.SetActive(false);
            
            base.Start();

            InteractableSimple.OnDestroyed += OnBallDestroyed;
            Collidable.OnTriggerEntered += OnBallTrigger;
        }

        protected override void OnDestroy() {
            InteractableSimple.OnDestroyed -= OnBallDestroyed;
            base.OnDestroy();
        }

        private void OnBallTrigger(Collidable collidable, Collider other) {
            if (!_isGameStarted) return;
            
            collidable.Stop();
            var interactable = collidable.GetComponent<InteractableSimple>();
            interactable.Bang(true);
            StartCoroutine(WaitAndWin());
        }

        private IEnumerator WaitAndWin() {
            yield return new WaitForSeconds(_waitBeforeWin);
            GameEvent.Current = GameState.STOP;
        }

        private void OnBallDestroyed(InteractableSimple obj) {
            _balls.Remove(obj);
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var objs = _balls.Select(b => b.transform).Append(_ballTpl.transform).Append(_goal);
            SetSizes(_gameField.Scale, objs);
            foreach (var ballSpawn in _ballSpawns) {
                var pos = ballSpawn.transform.localPosition;
                var z = _initialSpawnZ[ballSpawn] * _gameField.Scale;
                pos.z = Mathf.Min(-Prefs.Sandbox.OffsetMaxDepth, z);
                ballSpawn.transform.localPosition = pos;
            }
        }

        private IEnumerator SpawningBalls(Transform spawn) {
            while (true) {
                var ball = Instantiate(_ballTpl, _ballTpl.transform.parent, false);
                ball.transform.position = spawn.position;
                ball.transform.rotation = spawn.rotation;
                var rigid = ball.GetComponent<Rigidbody>();
                var velocity = _ballStartVelocity * _gameField.Scale;
                rigid.velocity = spawn.forward * velocity;
                rigid.mass /= _gameField.Scale;
                ball.gameObject.SetActive(true);
                
                _balls.Add(ball);
                yield return new WaitForSeconds(_ballSpawnOffset);
            }
        }

        private bool SpawnGoal(Transform ballSpawn) {
            var boundsNoCreatGoal = new Bounds(
                ballSpawn.position, 
                math.abs(ballSpawn.forward * 10f + ballSpawn.right * _areaForNoGoals + ballSpawn.up * 10f) * _gameField.Scale 
            );
            
            var found = _goalSpawns.GetRandomSpawn(out var pos, out var rot, new []{boundsNoCreatGoal});
            if (found) {
                _goal.position = pos;
                _goal.rotation = rot;
                _goal.gameObject.SetActive(true);
            }

            return found;
        }

        private void ClearBalls() {
            foreach (var ball in _balls) {
                ball.Dead();
            }
            _balls.Clear();
        }

        protected override void StartGame() {
            ClearBalls();
            base.StartGame();

            Transform ballSpawn;
            do {
                ballSpawn = _ballSpawns.Random().Spawns.Random();
            } while (!SpawnGoal(ballSpawn));
            
            StartCoroutine(nameof(SpawningBalls), ballSpawn);
        }

        protected override void StopGame() {
            StopCoroutine(nameof(SpawningBalls));
            base.StopGame();
        }
    }
}