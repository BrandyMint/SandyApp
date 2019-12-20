using System.Linq;
using Games.Common;
using Games.Common.ColliderGenerator;
using Games.Common.Game;
using Games.Common.GameFindObject;
using Games.PingPong.Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Games.Football {
    public class GameFootball : BaseGame {
        [SerializeField] protected Interactable _tplBall;
        [SerializeField] protected SpawnArea _spawnsBall;
        [SerializeField] protected SpawnArea _spawnsGoals;
        [SerializeField] protected Transform[] _goals;
        [SerializeField] private float _startBallVelocity = 0.5f;
        [SerializeField] private int _maxScore = 7;
        [SerializeField] private PolygonCollider2D _handsCollider;
        [SerializeField] private PolygonCollider2D _mouseCollider;
        [SerializeField] private int _mouseDebugDataSize = 128;
        [SerializeField] private float _mouseDebugSize = 0.1f;
        
        protected Interactable _ball;
        private int _lastPlayerMakeGoal;
        private Texture2D _handsTexture;

        private readonly ColliderGenerator _colliderGenerator = new ColliderGenerator();
        private readonly DataHandsByteArray _colliderGeneratorData = new DataHandsByteArray();
        private readonly DataMouse _colliderGeneratorDataMouse = new DataMouse();
        private readonly OutputPolygonCollider2D _colliderGeneratorOutput = new OutputPolygonCollider2D();
        private readonly OutputPolygonCollider2D _colliderGeneratorOutputMouse = new OutputPolygonCollider2D();
        

        protected override void Start() {
            _colliderGeneratorOutput.collider = _handsCollider;
            _colliderGeneratorOutputMouse.collider = _mouseCollider;
            
            _testMouseModeHold = true;
            SaveInitialSizes(_goals.Append(_tplBall.transform));
            _tplBall.Show(false);
            
            base.Start();

            Interactable.OnDestroyed += SpawnBall;
            Collidable.OnCollisionEntered2D += OnCollisionEntered;
        }

        protected override void OnDestroy() {
            Interactable.OnDestroyed -= SpawnBall;
            Collidable.OnCollisionEntered2D -= OnCollisionEntered;
            base.OnDestroy();
        }

        protected override void StartGame() {
            _colliderGeneratorOutput.Clear();
            _lastPlayerMakeGoal = Random.Range(0, GameScore.PlayerScore.Count);
            base.StartGame();
            SpawnBall();
        }

        protected override void StopGame() {
            _colliderGeneratorOutput.Clear();
            base.StopGame();
        }

        private void OnCollisionEntered(Collidable collidable, Collision2D collision) {
            if (!_isGameStarted) return;
            
            if (collision.gameObject.CompareTag("Goal")) {
                _ball.Bang(true);
                collidable.Stop();
                var player = _gameField.PlayerField(_gameField.ViewportFromWorld(collidable.transform.position));
                if (player >= 0) {
                    _lastPlayerMakeGoal = GameScore.PlayerScore.Count - player - 1;
                    ++GameScore.PlayerScore[_lastPlayerMakeGoal];
                }
            } else if (collision.gameObject.CompareTag("Player")) {
                _ball.PlayAudioBang(false);
                var speed = _gameField.Scale * _startBallVelocity;

                if (collidable.Velocity.magnitude < speed * 2) {
                    var normal = Vector2.zero;
                    foreach (var contact in collision.contacts) {
                        normal += contact.normal / collision.contacts.Length;
                    }
                    
                    collidable.Velocity += (Vector3)normal * speed;
                }
            }
        }

        protected override void SetSizes(float dist) {
            base.SetSizes(dist);
            var objs = _goals.Append(_tplBall.transform);
            if (_ball != null)
                objs.Append(_ball.transform);
            SetSizes(_gameField.Scale, objs);

            var i = 0;
            foreach (var goal in _goals) {
                var spawn = _spawnsGoals.Spawns.Skip(i++).First();
                goal.position = spawn.position;
                goal.rotation = spawn.rotation;
            }
        }

        private void SpawnBall(Interactable deadBall = null) {
            if (!_isGameStarted) return;

            for (int i = 0; i < GameScore.PlayerScore.Count; ++i) {
                if (GameScore.PlayerScore[i] >= _maxScore) {
                    GameEvent.Current = GameState.STOP;
                    return;
                }
            }

            while (true) {
                if (_spawnsBall.GetRandomSpawn(out var pos, out var rot)) {
                    if (_gameField.PlayerField(_gameField.ViewportFromWorld(pos)) != _lastPlayerMakeGoal) {
                        _ball = Instantiate(_tplBall, _tplBall.transform.parent);
                        _ball.transform.position = pos;
                        _ball.transform.rotation = rot;
                        _ball.Show(true);
                        return;
                    }
                }
            }
        }

        protected virtual void FixedUpdate() {
            if (Input.GetMouseButton(0) && _isGameStarted) {
                _colliderGeneratorDataMouse.CircleSize = _mouseDebugSize * _mouseDebugDataSize;
                var dataSize = new Vector2Int((int) (_mouseDebugDataSize * _cam.aspect), _mouseDebugDataSize);
                _colliderGeneratorDataMouse.Rect 
                    = _colliderGeneratorOutputMouse.SourceRect
                        = new RectInt(Vector2Int.zero, dataSize);
                
                _colliderGeneratorDataMouse.MousePos = new Vector2(
                    (int) (Input.mousePosition.x / _cam.pixelWidth * dataSize.x),
                    (int) (Input.mousePosition.y / _cam.pixelHeight * dataSize.y)
                );
                
                _colliderGeneratorOutputMouse.Clear();
                _colliderGenerator.Generate(_colliderGeneratorDataMouse, _colliderGeneratorOutputMouse);
                _mouseCollider.enabled = !_colliderGeneratorOutputMouse.IsEmpty();
            } else {
                if (!_colliderGeneratorOutputMouse.IsEmpty()) {
                    _colliderGeneratorOutputMouse.Clear();
                    _mouseCollider.enabled = false;
                }
            }
        }

        protected override void ProcessDepthFrame() {
            if (!_isGameStarted) return;

            _colliderGeneratorData.Rect 
                = _colliderGeneratorOutput.SourceRect
                    = new RectInt(Vector2Int.zero, new Vector2Int(_depthSize.x, _depthSize.y));
            _colliderGeneratorData.arr = _depth.o;
            _colliderGeneratorOutput.Clear();
            _colliderGenerator.Generate(_colliderGeneratorData, _colliderGeneratorOutput);
            _handsCollider.enabled = !_colliderGeneratorOutput.IsEmpty();
        }
    }
}