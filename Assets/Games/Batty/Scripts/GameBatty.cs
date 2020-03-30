using System.Collections.Generic;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Batty {
    public class GameBatty : BaseGameWithHandsRaycast {
        [SerializeField] protected InteractableSimple _tplBall;
        [SerializeField] protected Transform _player;
        [SerializeField] protected Bricks _bricks;
        [SerializeField] private float _startBallVelocity = 0.5f;
        [SerializeField] private float _handsWindowWidth = 0.1f;
        [Header("when debug hold 'W' to win")]
        [SerializeField] private bool _debugMod = false;
        
        protected InteractableSimple _ball;
        private readonly List<Vector2> _handsPoints = new List<Vector2>();
        private float _handsYPos;
        private float _playerYPosViewport;

        protected override void Start() {
            _testMouseModeHold = true;
            SaveInitialSizes(_player, _tplBall);
            _tplBall.Show(false);
            _playerYPosViewport = _gameField.ViewportFromWorld(_player.position).y;
            
            base.Start();
            _handsRaycaster.OnPreProcessDepthFrame += PreprocessHands;
            _handsRaycaster.OnPostProcessDepthFrame += PostProcessHands;
            Collidable.OnCollisionEntered2D += OnCollisionEntered;
        }

        protected override void OnDestroy() {
            Collidable.OnCollisionEntered2D -= OnCollisionEntered;
            base.OnDestroy();
        }

        private void ShowPlayer(bool show) {
            _player.gameObject.SetActive(show);
            SetPlayer( 0f);
        }

        protected override void StartGame() {
            GameScore.Lost = false;
            ShowPlayer(true);
            _bricks.Show(true);
            base.StartGame();
            SpawnBall();
        }

        protected override void StopGame() {
            base.StopGame();
            ShowPlayer(false);
        }

        private void OnCollisionEntered(Collidable collidable, Collision2D collision) {
            if (!_isGameStarted) return;
            
            if (collision.gameObject.CompareTag("Goal")) {
                var brick = collision.gameObject.GetComponentInParent<InteractableSimple>();
                brick.Bang(true);
                ++GameScore.Score;
                if (GameScore.Score >= _bricks.Count || (_debugMod && Input.GetKey(KeyCode.W))) {
                    GameEvent.Current = GameState.STOP;
                    _ball.Dead();
                }
                collidable.MakeAbsoluteBounceClampNormalsFor9Dirs(collision);
            } else if (collision.gameObject.CompareTag("Finish")) {
                GameScore.Lost = true;
                _ball.Bang(false);
                collidable.Stop();
                GameEvent.Current = GameState.STOP;
            } else {
                _ball.PlayAudioBang(true);
                collidable.MakeAbsoluteBounceClampNormalsFor9Dirs(collision);
            }
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var objs = Enumerable.Repeat(_player, 1).Append(_tplBall.transform);
            if (_ball != null)
                objs.Append(_ball.transform);
            SetSizes(_gameField.Scale, objs);
        }

        private void SpawnBall() {
            if (!_isGameStarted) return;
            
            if (GameScore.Lost) {
                GameEvent.Current = GameState.STOP;
                return;
            }
            
            SpawnArea.AnyGetRandomSpawn(out var pos, out var rot);
            _ball = Instantiate(_tplBall, _tplBall.transform.parent);
            _ball.transform.position = pos;
            _ball.transform.rotation = rot;
            _ball.Show(true);
            var collidable = _ball.GetComponent<Collidable>();
            collidable.Velocity = rot * (_startBallVelocity * _gameField.Scale * Vector3.forward);
        }

        private void SetPlayer(float fieldX) {
            var player = _player;
            var max = 0.5f - Mathf.Abs(_gameField.transform.InverseTransformVector(player.lossyScale).x) / 2f;
            var fieldPos = new Vector3{
                x = Mathf.Clamp(fieldX, -max, max), 
                y = _gameField.LocalFromViewport(new Vector2(0.5f, GetPlayerYPosVieport())).y,
                z = 0f
            };
            player.position = _gameField.transform.TransformPoint(fieldPos);
        }
        
        protected override void Update() {
            if (!_isGameStarted) return;
            PreprocessHands();
            base.Update();
            PostProcessHands();
        }

        private void PreprocessHands() {
            _handsPoints.Clear();
        }

        private void PostProcessHands() {
            if (_handsPoints.Any()) {
                var playerPos = _handsYPos;
                var handViewPosX = _handsPoints
                    .Where(p => p.y > playerPos - _handsWindowWidth && p.y < playerPos + _handsWindowWidth)
                    .Average(p => p.x);
                var fieldPos = _gameField.LocalFromViewport(new Vector2(handViewPosX, _handsYPos));
                SetPlayer(fieldPos.x);
            }
        }

        protected override void Fire(Ray ray, Vector2 viewPos) {
            var hands = _handsPoints;
            if (!hands.Any()) {
                _handsYPos = viewPos.y;
            } else {
                var playerYPosViewport = GetPlayerYPosVieport();
                if (Mathf.Abs(viewPos.y - playerYPosViewport)
                < Mathf.Abs(_handsYPos - playerYPosViewport))
                    _handsYPos = viewPos.y;
            }
            hands.Add(viewPos);
        }

        private float GetPlayerYPosVieport() {
            if (Prefs.App.FlipVertical) {
                return 1f - _playerYPosViewport;
            }

            return _playerYPosViewport;
        }
    }
}