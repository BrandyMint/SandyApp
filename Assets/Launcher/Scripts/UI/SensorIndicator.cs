using System;
using DepthSensor;
using UnityEngine;
using UnityEngine.UI;

namespace Launcher.UI {
    public class SensorIndicator : MonoBehaviour {
        [SerializeField] private StateBind[] _indicators;
        [SerializeField] private Text _txtDeviceName;
        
        [Serializable]
        public struct StateBind {
            public State state;
            public GameObject indicator;
        }
        
        public enum State {
            CONNECTED,
            DISCONNECTED,
            INITIALIZING
        }

        private void Awake() {
            SetIndicator(State.INITIALIZING);
        }

        private void FixedUpdate() {
            if (DepthSensorManager.IsInitialized()) {
                _txtDeviceName.text = DepthSensorManager.Instance.Device.Name;
                SetIndicator(State.CONNECTED);
            } else 
            if (DepthSensorManager.Initializing()) {
                SetIndicator(State.INITIALIZING);
            } else {
                SetIndicator(State.DISCONNECTED);
            }
        }

        private void SetIndicator(State state) {
            foreach (var indicator in _indicators) {
                indicator.indicator.SetActive(indicator.state == state);
            }
        }
    }
}