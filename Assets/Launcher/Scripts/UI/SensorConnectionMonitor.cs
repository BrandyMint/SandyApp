using DepthSensor;
using UINotify;
using UnityEngine;
using UnityEngine.UI;

namespace Launcher.UI {
    public class SensorConnectionMonitor : MonoBehaviour {
        private const string _TXT_CONNECTING = "Подключение сенсора...";
        private const string _TXT_CONNECT_FAIL = "Сенсор не подключен!";
        
        [SerializeField] private GameObject _btnTryInit;
        
        private Notify.Control _notify;
        private Style _currentNotify;
        
        private void FixedUpdate() {
            var connected = DepthSensorManager.IsInitialized();
            var initializing = DepthSensorManager.Initializing();
            if (connected) {
                if (_notify != null) {
                    _notify.Hide();
                    _notify = null;
                }
                return;
            }

            if (initializing && _notify != null && _currentNotify != Style.INFO) {
                _currentNotify = Style.INFO;
                _notify.Set(new Notify.Params {
                    style = Style.INFO,
                    time = LifeTime.INFINITY,
                    text = _TXT_CONNECTING
                });
            }
            
            if (!initializing && (_notify == null || _currentNotify != Style.FAIL)) {
                _currentNotify = Style.FAIL;
                var btn = Instantiate(_btnTryInit, null, false);
                btn.GetComponentInChildren<Button>().onClick.AddListener(OnBtnTryInit);
                var p = new Notify.Params {
                    style = Style.FAIL,
                    time = LifeTime.INFINITY,
                    title = _TXT_CONNECT_FAIL,
                    content = btn
                };
                if (_notify == null)
                    _notify = Notify.Show(p);
                else
                    _notify.Set(p);
            }
        }

        private static void OnBtnTryInit() {
            DepthSensorManager.Instance.Stop();
            DepthSensorManager.Instance.TryInit();
        }
    }
}