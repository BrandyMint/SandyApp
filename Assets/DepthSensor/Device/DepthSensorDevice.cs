using System.Collections;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensor.Device {
    public abstract class DepthSensorDevice {
        public readonly Sensor<ushort> Depth;
        public readonly Sensor<byte> Index;
        public readonly ColorByteSensor Color;
        public readonly BodySensor Body;
        public readonly Sensor<Vector2> MapDepthToCamera;
        public readonly string Platform;
        
        protected readonly Sensor<ushort>.Internal _internalDepth;
        protected readonly Sensor<byte>.Internal _internalIndex;
        protected readonly Sensor<byte>.Internal _internalColor;
        protected readonly Sensor<Vector2>.Internal _internalMapDepthToCamera;

        private readonly bool _isInitialised;

        protected class InitInfo {
            public Sensor<ushort> Depth;
            public Sensor<byte> Index;
            public ColorByteSensor Color;
            public BodySensor Body;
            public Sensor<Vector2> MapDepthToColor;
        }

        protected DepthSensorDevice(string platform, InitInfo initInfo) {
            Platform = platform;
            Depth = initInfo.Depth;
            Index = initInfo.Index;
            Color = initInfo.Color;
            Body = initInfo.Body;
            MapDepthToCamera = initInfo.MapDepthToColor;

            _internalDepth = new Sensor<ushort>.Internal(Depth);
            _internalDepth.SetOnActiveChanged(SensorActiveChanged);
            _internalIndex = new Sensor<byte>.Internal(Index);
            _internalIndex.SetOnActiveChanged(SensorActiveChanged);
            _internalColor = new Sensor<byte>.Internal(Color);
            _internalColor.SetOnActiveChanged(SensorActiveChanged);
            _internalMapDepthToCamera = new Sensor<Vector2>.Internal(MapDepthToCamera);
            _internalMapDepthToCamera.SetOnActiveChanged(SensorActiveChanged);
        }
        
        public abstract bool IsAvailable();
        public abstract Vector2 CameraPosToDepthMapPos(Vector3 pos);
        public abstract Vector2 CameraPosToColorMapPos(Vector3 pos);
        public abstract Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth);

        protected abstract void SensorActiveChanged<T>(Sensor<T> sensor);
        protected abstract IEnumerator Update();
        protected abstract void Close();
        
        public class Internal {
            private readonly DepthSensorDevice _device;

            protected internal Internal(DepthSensorDevice device) {
                _device = device;
            }

            protected internal IEnumerator Update() {
                return _device.Update();
            }
            
            protected internal void Close() {
                _device.Close();
            }
        }
    }
}