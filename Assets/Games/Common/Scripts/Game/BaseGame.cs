using System.Collections.Generic;
using System.Linq;
using DepthSensorSandbox.Visualisation;
using UnityEngine;

namespace Games.Common.Game {
    public class BaseGame : MonoBehaviour {
        [SerializeField] protected Camera _cam;
        [SerializeField] protected GameField _gameField;
        
        protected bool _isGameStarted;
        protected readonly Dictionary<string, Vector3> _initialSizes = new Dictionary<string, Vector3>();

        protected virtual void Start() {
            Prefs.Calibration.OnChanged += OnCalibrationChanged;
            Prefs.Sandbox.OnChanged += OnCalibrationChanged;
            OnCalibrationChanged();

            GameEvent.OnStart += StartGame;
            GameEvent.OnStop += StopGame;
        }

        protected virtual void OnDestroy() {
            GameEvent.OnStart -= StartGame;
            GameEvent.OnStop -= StopGame;
            
            Prefs.Sandbox.OnChanged -= OnCalibrationChanged;
            Prefs.Calibration.OnChanged -= OnCalibrationChanged;
        }

        protected virtual void OnCalibrationChanged() {
            var cam = _cam.GetComponent<SandboxCamera>();
            if (cam != null) {
                cam.OnCalibrationChanged();
                SetSizes(Prefs.Sandbox.ZeroDepth);
            } else {
                SetSizes(1.66f); //for testing
            }
        }

        protected virtual void SetSizes(float dist) {
            _gameField.AlignToCamera(_cam, dist);
        }

        protected void SaveInitialSizes(params Component[] objs) {
            SaveInitialSizes(objs.AsEnumerable());
        }

        protected void SaveInitialSizes(IEnumerable<Component> objs) {
            foreach (var obj in objs) {
                _initialSizes[obj.name] = obj.transform.localScale;
            }
        }

        protected void SetSizes(float mult, params Component[] objs) {
            SetSizes(mult, objs.AsEnumerable());
        }

        protected void SetSizes(float mult, IEnumerable<Component> objs) {
            foreach (var obj in objs) {
                var initial = _initialSizes.FirstOrDefault(kv => kv.Key.Contains(obj.name));
                obj.transform.localScale = initial.Value * mult;
            }
        }

        protected virtual void StartGame() {
            GameScore.Score = 0;
            for (int i = 0; i < GameScore.PlayerScore.Count; ++i) {
                GameScore.PlayerScore[i] = 0;
            }
            _isGameStarted = true;;
        }

        protected virtual void StopGame() {
            _isGameStarted = false;
        }
    }
}