using System;
using System.Collections;
using System.Threading;
using DepthSensor;
using DepthSensor.Device;
using DepthSensor.Sensor;

namespace BgConveyer {
    public class DepthSensorConveyer : BgConveyer {
        //private Stopwatch watch = Stopwatch.StartNew();
        public const string TASK_NAME = "WaitNewFrame";
        private readonly AutoResetEvent _newFrameEvent = new AutoResetEvent(false);
        public event Action OnNoFrame;

        public DepthSensorConveyer() {
            AddToBG(TASK_NAME, null, SleepIfNeed());
            _defaultAfter = TASK_NAME;
        }

        private void Start() {
            DepthSensorManager.OnInitialized += OnDepthSensorInit;
            if (DepthSensorManager.IsInitialized())
                OnDepthSensorInit();
        }

        protected new void OnDestroy() {
            DepthSensorManager.OnInitialized -= OnDepthSensorInit;
            if (DepthSensorManager.IsInitialized()) {
                UnSubscribe(DepthSensorManager.Instance.Device);
            }
            _newFrameEvent.Dispose();
            base.OnDestroy();
        }

        private void OnDepthSensorInit() {
            Run();
            DepthSensorManager.Instance.Device.OnClose += UnSubscribe;
            DepthSensorManager.Instance.Device.Depth.OnNewFrameBackground += OnNewFrame;
        }

        private void UnSubscribe(DepthSensorDevice device) {
            Stop();
            device.OnClose -= UnSubscribe;
            device.Depth.OnNewFrameBackground -= OnNewFrame;
        }

        private void OnNewFrame(ISensor abstractBuffer) {
            _newFrameEvent.Set();
        }
        
        private IEnumerator SleepIfNeed() {
            while (true) {
                if (!_newFrameEvent.WaitOne(300)) {
                    OnNoFrame?.Invoke();
                    yield return new ToNewIteration();
                } else {
                    /*watch.Reset();
                    watch.Start();*/
                    yield return null;
                    /*watch.Stop();
                    UnityEngine.Debug.Log("Sleep " + watch.ElapsedMilliseconds);*/
                }
            }
        }
    }
}