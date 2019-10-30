﻿using System;
using System.Collections;
using System.Threading;
using DepthSensor;
using DepthSensor.Sensor;

namespace BgConveyer {
    public class DepthSensorConveyer : BgConveyer {
        //private Stopwatch watch = Stopwatch.StartNew();
        public const string TASK_NAME = "WaitNewFrame";
        private DepthSensorManager _dsm;
        private readonly AutoResetEvent _newFrameEvent = new AutoResetEvent(false);
        public event Action OnNoFrame;

        public DepthSensorConveyer() {
            AddToBG(TASK_NAME, null, SleepIfNeed());
            _defaultAfter = TASK_NAME;
        }

        private void Start() {
            _dsm = DepthSensorManager.Instance;
            if (_dsm != null)
                _dsm.OnInitialized += OnDepthSensorInit;
        }

        protected new void OnDestroy() {
            if (DepthSensorManager.IsInitialized()) {
                _dsm.Device.Depth.OnNewFrameBackground -= OnNewFrame;
                _dsm.OnInitialized -= OnDepthSensorInit;
            }
            _newFrameEvent.Dispose();
            base.OnDestroy();
        }

        private void OnDepthSensorInit() {
            Run();
            _dsm.Device.Depth.OnNewFrameBackground += OnNewFrame;
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