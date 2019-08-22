using System.Collections;
using DepthSensor.Sensor;
using UnityEngine;

namespace DepthSensor.Device {
    public abstract class DepthSensorDevice {
        public readonly SensorDepth Depth;
        public readonly SensorIndex Index;
        public readonly SensorColor Color;
        public readonly SensorBody Body;
        public readonly SensorMapDepthToCamera MapDepthToCamera;
        public readonly string Platform;
        
        
        protected InitInfo _initInfo;
        protected readonly SensorDepth.Internal _internalDepth;
        protected readonly SensorIndex.Internal _internalIndex;
        protected readonly SensorColor.Internal _internalColor;
        protected readonly SensorBody.Internal _internalBody;
        protected readonly SensorMapDepthToCamera.Internal _internalMapDepthToCamera;

        private readonly bool _isInitialised;

        protected class InitInfo {
            public SensorDepth Depth;
            public SensorIndex SensorIndex;
            public SensorColor SensorColor;
            public SensorBody Body;
            public SensorMapDepthToCamera MapDepthToCamera;
        }

        protected DepthSensorDevice(string platform, InitInfo initInfo) {
            Platform = platform;
            Depth = initInfo.Depth ?? new SensorDepth(false);
            Index = initInfo.SensorIndex ?? new SensorIndex(false);
            Color = initInfo.SensorColor ?? new SensorColor(false);
            Body = initInfo.Body ?? new SensorBody(false);
            MapDepthToCamera = initInfo.MapDepthToCamera ?? new SensorMapDepthToCamera(false);

            _internalDepth = new SensorDepth.Internal(Depth);
            _internalDepth.SetOnActiveChanged(SensorActiveChanged);
            _internalIndex = new SensorIndex.Internal(Index);
            _internalIndex.SetOnActiveChanged(SensorActiveChanged);
            _internalColor = new SensorColor.Internal(Color);
            _internalColor.SetOnActiveChanged(SensorActiveChanged);
            _internalBody  = new SensorBody.Internal(Body);
            _internalBody.SetOnActiveChanged(SensorActiveChanged);
            _internalMapDepthToCamera = new SensorMapDepthToCamera.Internal(MapDepthToCamera);
            _internalMapDepthToCamera.SetOnActiveChanged(SensorActiveChanged);

            _initInfo = initInfo;
        }

        public abstract bool IsAvailable();
        public abstract Vector2 CameraPosToDepthMapPos(Vector3 pos);
        public abstract Vector2 CameraPosToColorMapPos(Vector3 pos);
        public abstract Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth);

        protected abstract void SensorActiveChanged(AbstractSensor sensor);
        protected abstract IEnumerator Update();

        protected virtual void Close() {
            Depth.Dispose();
            Index.Dispose();
            Color.Dispose();
            Body.Dispose();
            MapDepthToCamera.Dispose();
        }
        
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