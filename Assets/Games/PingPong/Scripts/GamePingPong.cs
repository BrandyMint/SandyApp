using System.Collections.Generic;
using System.Linq;
using Games.Common;
using Games.Common.Game;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.PingPong.Scripts {
    public class GamePingPong : BaseGame {
        [SerializeField] protected Interactable _tplBall;
        [SerializeField] protected Transform[] _players;
        [SerializeField] private float _startBallVelocity = 0.5f;
        [SerializeField] private float _handsWindowWidth = 0.1f;
        [SerializeField] private int _maxScore = 7;
        
        
        protected Interactable _ball;
        private readonly Dictionary<string, Vector3> _initialSizes = new Dictionary<string, Vector3>();
        private List<Vector2>[] _handsPoints;
        private float[] _handsXPos;
        private float[] _playerXPosViewport;

        protected override void Start() {
            _testMouseModeHold = true;
            SaveInitialSizes(_players.Append(_tplBall.transform));
            _tplBall.Show(false);
            _handsPoints = new List<Vector2>[GameScore.PlayerScore.Count];
            _handsXPos = new float[GameScore.PlayerScore.Count];
            _playerXPosViewport = new float[GameScore.PlayerScore.Count];
            for (int i = 0; i < _handsPoints.Length; ++i) {
                _handsPoints[i] = new List<Vector2>();
            }
            PreprocessHands();
            ShowPlayers(true);
            ShowPlayers(false);
            
            base.Start();

            Interactable.OnDestroyed += SpawnBall;
            Collidable.OnCollisionEntered2D += OnCollisionEntered;
        }

        protected override void OnDestroy() {
            Interactable.OnDestroyed -= SpawnBall;
            Collidable.OnCollisionEntered2D -= OnCollisionEntered;
            base.OnDestroy();
        }

        private void ShowPlayers(bool show) {
            foreach (var player in _players) {
                player.gameObject.SetActive(show);
            }
            if (show) {
                for (int i = 0; i < _players.Length; ++i) {
                    SetPlayer(i, 0f);
                }
            }
        }

        protected override void StartGame() {
            ShowPlayers(true);
            base.StartGame();
            SpawnBall();
        }

        protected override void StopGame() {
            base.StopGame();
            ShowPlayers(false);
        }

        private void OnCollisionEntered(Collidable collidable, Collision2D collision) {
            if (!_isGameStarted) return;
            
            if (collision.gameObject.CompareTag("Goal")) {
                _ball.Bang(true);
                collidable.Stop();
                var player = _gameField.PlayerField(_gameField.ViewportFromWorld(collidable.transform.position));
                if (player >= 0) {
                    ++GameScore.PlayerScore[GameScore.PlayerScore.Count - player - 1];
                }
            } else {
                _ball.PlayAudioBang(false);
                var normal = Vector2.zero;
                var point = Vector2.zero;
                foreach (var contact in collision.contacts) {
                    normal += contact.normal / collision.contacts.Length;
                    point += contact.point / collision.contacts.Length;
                }
                var velocity = collidable.LastFrameVelocity;
                var dir = point - (Vector2) collidable.transform.position;
                if (Vector2.Dot(velocity, dir) >= 0f)
                    collidable.Velocity = Vector3.Reflect(velocity, normal);
            }
        }

        private void SaveInitialSizes(IEnumerable<Component> objs) {
            foreach (var obj in objs) {
                _initialSizes[obj.name] = obj.transform.localScale;
            }
        }

        private void SetSizes(float mult, IEnumerable<Component> objs) {
            foreach (var obj in objs) {
                var initial = _initialSizes.FirstOrDefault(kv => kv.Key.Contains(obj.name));
                obj.transform.localScale = initial.Value * mult;
            }
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var objs = _players.Append(_tplBall.transform);
            if (_ball != null)
                objs.Append(_ball.transform);
            SetSizes(_gameField.Scale, objs);
        }

        private void SpawnBall(Interactable deadBall = null) {
            if (!_isGameStarted) return;

            for (int i = 0; i < _players.Length; ++i) {
                if (GameScore.PlayerScore[i] >= _maxScore) {
                    GameEvent.Current = GameState.STOP;
                    return;
                }
            }
            
            SpawnArea.AnyGetRandomSpawn(out var pos, out var rot);
            _ball = Instantiate(_tplBall, _tplBall.transform.parent);
            _ball.transform.position = pos;
            _ball.transform.rotation = rot;
            _ball.Show(true);
            var collidable = _ball.GetComponent<Collidable>();
            collidable.Velocity = rot * (_startBallVelocity * _gameField.Scale * Vector3.forward);
        }

        private void SetPlayer(int i, float fieldY) {
            var player = _players[i];
            var max = 0.5f - Mathf.Abs(_gameField.transform.InverseTransformVector(player.lossyScale).y) / 2f;
            var fieldPos = new Vector3{
                x = _gameField.LocalFromViewport(new Vector2(_playerXPosViewport[i], 0.5f)).x, 
                y = Mathf.Clamp(fieldY, -max, max),
                z = 0f
            };
            player.position = _gameField.transform.TransformPoint(fieldPos);
            
            var otherPlayer = _players.First(p => p != player);
            if (Vector3.Dot(player.right, otherPlayer.position - player.position) < 0) {
                //swap rotations
                var rot = player.rotation;
                player.rotation = otherPlayer.rotation;
                otherPlayer.rotation = rot;
            }
        }
        
        protected override void Update() {
            if (!_isGameStarted) return;
            PreprocessHands();
            base.Update();
            PostProcessHands();
        }

        protected override void ProcessDepthFrame() {
            if (!_isGameStarted) return;
            PreprocessHands();
            base.ProcessDepthFrame();
            PostProcessHands();
        }

        private void PreprocessHands() {
            foreach (var list in _handsPoints) {
                list.Clear();
            }

            if (_gameField.PlayerField(_gameField.ViewportFromWorld(_players[0].position)) != 0) {
                var player = _players[0];
                _players[0] = _players[1];
                _players[1] = player;
            }

            for (int i = 0; i < _players.Length; ++i) {
                _playerXPosViewport[i] = _gameField.ViewportFromWorld(_players[i].position).x;
            }
        }

        private void PostProcessHands() {
            for (int i = 0; i < _players.Length; ++i) {
                if (_handsPoints[i].Any()) {
                    var playerPos = _handsXPos[i];
                    var handViewPosY = _handsPoints[i]
                        .Where(p => p.x > playerPos - _handsWindowWidth && p.x < playerPos + _handsWindowWidth)
                        .Average(p => p.y);
                    var fieldPos = _gameField.LocalFromViewport(new Vector2(_handsXPos[i], handViewPosY));
                    SetPlayer(i, fieldPos.y);
                }
            }
        }

        protected override void Fire(Vector2 viewPos) {
            var player = _gameField.PlayerField(viewPos);
            if (player >= 0) {
                var hands = _handsPoints[player];
                if (!hands.Any()) {
                    _handsXPos[player] = viewPos.x;
                } else {
                    if (Mathf.Abs(viewPos.x - _playerXPosViewport[player])
                    < Mathf.Abs(_handsXPos[player] - _playerXPosViewport[player]))
                        _handsXPos[player] = viewPos.x;
                }
                hands.Add(viewPos);
            }
        }
    }
}