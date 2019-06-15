using System.Collections;
using System.Threading;
using DepthSensor;
using DepthSensor.Sensor;
using UnityEngine;

namespace BgConveyer {
    public class KinectConveyer : BgConveyer {
        //private Stopwatch watch = Stopwatch.StartNew();
        private DepthSensorManager _dsm;
        private readonly AutoResetEvent _newFrameEvent = new AutoResetEvent(false);

        public KinectConveyer() {
            AddToBG("KinectConveyerSleepIfNeed", null, SleepIfNeed());
        }

        private void Start() {
            _dsm = DepthSensorManager.Instance;
            if (_dsm != null)
                _dsm.OnInitialized += OnDepthSensorInit;
        }

        protected new void OnDestroy() {
            if (DepthSensorManager.IsInitialized()) {
                _dsm.Device.Depth.OnNewFrame -= OnNewFrame;
                _dsm.OnInitialized -= OnDepthSensorInit;
            }
            _newFrameEvent.Dispose();
            base.OnDestroy();
        }

        private void OnDepthSensorInit() {
            Run();
            _dsm.Device.Depth.OnNewFrame += OnNewFrame;
        }

        private void OnNewFrame(Sensor<ushort> sensor) {
            _newFrameEvent.Set();
        }
        
        private IEnumerator SleepIfNeed() {
            while (true) {
                _newFrameEvent.WaitOne();
                /*watch.Reset();
                watch.Start();*/
                yield return null;
                /*watch.Stop();
                UnityEngine.Debug.Log("Sleep " + watch.ElapsedMilliseconds);*/                
            }
        }
    }
}