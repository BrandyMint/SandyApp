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

        private RecordManifest _manifest;

        public bool Recording => _manifest != null;
        
        public bool StartRecord(DepthSensorDevice device, string path, bool forceActivateAll = false) {
            StopRecord();
            var newDirCreated = false;
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
                newDirCreated = true;
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
            if (recording)
                return true;
            else {
                _manifest = null;
                if (newDirCreated)
                    Directory.Delete(path);
                return false;
            }
        }

        public void StopRecord() {
            if (_manifest != null) {
                StopSensorRecord(Depth, nameof(Depth));
                StopSensorRecord(Infrared, nameof(Infrared));
                StopSensorRecord(Color, nameof(Color));
                StopSensorRecord(MapDepthToCamera, nameof(MapDepthToCamera));
                _manifest.Save();
            }
        }

        private void StopSensorRecord<T>(SensorRecorder<T> recorder, string name) where T : IBuffer {
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