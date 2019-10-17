using Launcher.KeyMapping;
using Launcher.MultiMonitorSupport;
using UnityEngine;

namespace DepthSensorCalibration.HalfVisualization {
    public class HalfVisualizationControl : HalfBase {
        [SerializeField] private HalfBase[] _halfs = {};

        private readonly FillType[] _switchQueue = {
            FillType.BOTTOM,
            FillType.TOP,
            FillType.FULL,
            FillType.NONE
        };

        private int _currSwitch = -1;

        private void Start() {
            if (MultiMonitor.MonitorsCount == 1) {
                SwitchMode();
                KeyMapper.AddListener(KeyEvent.SWITCH_MODE, SwitchMode);
            } else {
                Fill = FillType.FULL;
            }
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.SWITCH_MODE, SwitchMode);
        }

        private void SwitchMode() {
            _currSwitch = (_currSwitch + 1) % _switchQueue.Length;
            Fill = _switchQueue[_currSwitch];
        }

        protected override void SetHalf(FillType type) {
            foreach (var half in _halfs) {
                half.Fill = type;
            }
        }
    }
}