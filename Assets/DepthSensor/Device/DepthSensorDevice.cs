using System.Collections;
using DepthSensor.Stream;
using UnityEngine;

namespace DepthSensor.Device {
    public abstract class DepthSensorDevice {
        public readonly DepthStream Depth;
        public readonly IndexStream Index;
        public readonly ColorStream Color;
        public readonly BodyStream Body;
        public readonly MapDepthToCameraStream MapDepthToCamera;
        public readonly string Platform;
        
        
        protected InitInfo _initInfo;
        protected readonly Stream.DepthStream.Internal _internalDepth;
        protected readonly IndexStream.Internal _internalIndex;
        protected readonly ColorStream.Internal _internalColor;
        protected readonly MapDepthToCameraStream.Internal _internalMapDepthToCamera;

        private readonly bool _isInitialised;

        protected class InitInfo {
            public DepthStream Depth;
            public IndexStream Index;
            public ColorStream Color;
            public BodyStream Body;
            public MapDepthToCameraStream MapDepthToColor;
        }

        protected DepthSensorDevice(string platform, InitInfo initInfo) {
            Platform = platform;
            Depth = initInfo.Depth ?? new DepthStream(false);
            Index = initInfo.Index ?? new IndexStream(false);
            Color = initInfo.Color ?? new ColorStream(false);
            Body = initInfo.Body ?? new BodyStream(false);
            MapDepthToCamera = initInfo.MapDepthToColor ?? new MapDepthToCameraStream(false);

            _internalDepth = new DepthStream.Internal(Depth);
            _internalDepth.SetOnActiveChanged(SensorActiveChanged);
            _internalIndex = new IndexStream.Internal(Index);
            _internalIndex.SetOnActiveChanged(SensorActiveChanged);
            _internalColor = new ColorStream.Internal(Color);
            _internalColor.SetOnActiveChanged(SensorActiveChanged);
            _internalMapDepthToCamera = new MapDepthToCameraStream.Internal(MapDepthToCamera);
            _internalMapDepthToCamera.SetOnActiveChanged(SensorActiveChanged);

            _initInfo = initInfo;
        }

        public virtual bool IsManualUpdate { get; set; }

        public abstract bool IsAvailable();
        public abstract Vector2 CameraPosToDepthMapPos(Vector3 pos);
        public abstract Vector2 CameraPosToColorMapPos(Vector3 pos);
        public abstract Vector2 DepthMapPosToColorMapPos(Vector2 pos, ushort depth);

        protected abstract void SensorActiveChanged(AbstractStream stream);
        protected abstract IEnumerator Update();
        public abstract void ManualUpdate();

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