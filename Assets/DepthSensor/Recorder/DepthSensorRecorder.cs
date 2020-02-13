using System;
using System.Diagnostics;
using System.IO;
using DepthSensor.Buffer;
using DepthSensor.Device;
using DepthSensor.Sensor;
using Unity.Mathematics;

namespace DepthSensor.Recorder {
    public class DepthSensorRecorder : IDisposable {
        private readonly SensorRecorderBuffer2D<ushort> Depth = new SensorRecorderBuffer2D<ushort>();
        private readonly SensorRecorderBuffer2D<byte> Infrared = new SensorRecorderBuffer2D<byte>();
        private readonly SensorRecorderBuffer2D<byte> Color = new SensorRecorderBuffer2D<byte>();
        private readonly SensorRecorderBuffer2D<half2> MapDepthToCamera = new SensorRecorderBuffer2D<half2>();
        private readonly RecorderCalibration Calibration = new RecorderCalibration();

        private RecordManifest _manifest;
        private bool _newDirCreated;
        private string _path;

        public bool Recording => Calibration.Working || _manifest != null;
        public long StoredBytes { get {
            if (Calibration.Working)
                return 0;
            return Depth.StoredBytes + Depth.StoredBytes + Color.StoredBytes + MapDepthToCamera.StoredBytes;
        } }

        public event Action OnFail;

        public DepthSensorRecorder() {
            Depth.OnFail += StopOnFail;
            Infrared.OnFail += StopOnFail;
            Color.OnFail += StopOnFail;
            MapDepthToCamera.OnFail += StopOnFail;
            Calibration.OnFail += StopOnFail;
        }

        public void StartRecord(DepthSensorDevice device, string path, bool forceActivateAll = false) {
            StopRecord();
            device.Depth.Active = true;
            if (RecorderCalibration.IsNeedCalibrate(device))
                FailIfNot(Calibration.Start(device, () => StartRecordAfterCalibration(device, path, forceActivateAll)));
            else {
                StartRecordAfterCalibration(device, path, forceActivateAll);
            }
        }

        private void StartRecordAfterCalibration(DepthSensorDevice device, string path, bool forceActivateAll = false) {
            _path = path;
            Calibration.Stop();
            _newDirCreated = false;
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
                _newDirCreated = true;
            }

            _manifest = new RecordManifest(path);
            _manifest.Reset();
            _manifest.DeviceName = device.Name;

            var timer = Stopwatch.StartNew();
            var r1 = StartRecordSensorIfNeed(Depth, device.Depth, nameof(Depth), timer, path, forceActivateAll);
            var r2 = StartRecordSensorIfNeed(Infrared, device.Infrared, nameof(Infrared), timer, path, forceActivateAll);
            var r3 = StartRecordSensorIfNeed(Color, device.Color, nameof(Color), timer, path, forceActivateAll);
            var r4 = StartRecordSensorIfNeed(MapDepthToCamera, device.MapDepthToCamera, nameof(MapDepthToCamera), timer, path, forceActivateAll);
            var recording = r1 || r2 || r3 || r4;
            FailIfNot(recording);
        }

        public void StopRecord() {
            StopRecordInternal(false);
        }

        public void StopRecordInternal(bool isFail) {
            Calibration.Stop();
            if (_manifest != null) {
                StopSensorRecord(Depth, nameof(Depth));
                StopSensorRecord(Infrared, nameof(Infrared));
                StopSensorRecord(Color, nameof(Color));
                StopSensorRecord(MapDepthToCamera, nameof(MapDepthToCamera));
                if (!isFail)
                    SaveConfigs();
                _manifest = null;
            }
            _newDirCreated = false;
        }

        private void SaveConfigs() {
            _manifest.Save();
            var calibration = new RecordCalibrationResult();
            if (calibration.Load())
                calibration.SaveCopyTo(_path);
        }

        private void StopOnFail() {
            var newDirCreated = _newDirCreated;
            StopRecordInternal(true);
            if (newDirCreated)
                Directory.Delete(_path);
            OnFail?.Invoke();
        }

        private void FailIfNot(bool success) {
            if (!success)
                StopOnFail();
        }

        private void StopSensorRecord<T>(SensorRecorder<T> recorder, string name) where T : AbstractBuffer {
            if (recorder.Recording) {
                var info = _manifest.Get<StreamInfo>(name);
                info.framesCount = recorder.StopRecord();
            }
        }

        private bool StartRecordSensorIfNeed<T>(SensorRecorder<T> recorder, ISensor<T> sensor,
            string name, Stopwatch timer, string path, bool forceActivateAll) where T : AbstractBuffer2D 
        {
            if (forceActivateAll)
                sensor.Active = true;
            if (sensor.Active) {
                var info = new StreamInfo();
                var buff = sensor.GetNewest();
                info.width = buff.width;
                info.height = buff.height;
                info.textureFormat = ((ITextureBuffer)buff).GetTexture().format;
                info.fps = sensor.FPS;
                var fullPath = Path.Combine(path, name);
                recorder.StartRecord(timer, sensor, fullPath);
                _manifest.Set(name, info);
                return true;
            }
            return false;
        }

        public void Dispose() {
            StopRecord();
            Depth?.Dispose();
            Infrared?.Dispose();
            Color?.Dispose();
            MapDepthToCamera?.Dispose();
        }
    }
}