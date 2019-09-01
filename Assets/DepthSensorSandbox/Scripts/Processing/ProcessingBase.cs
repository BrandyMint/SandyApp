using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public abstract class ProcessingBase {
        public const ushort INVALID_DEPTH = 0;
        
        public bool OnlyRawBuffersIsInput = true;
        public bool Active = true;
        
        protected DepthBuffer[] _rawBuffers;
        protected DepthBuffer _inOut;

        public void Process(DepthBuffer[] rawBuffers, DepthBuffer inOut) {
            if (Active) {
                _rawBuffers = rawBuffers;
                _inOut = inOut;
                ProcessInternal();
            }
        }

        protected abstract void ProcessInternal();

        protected bool ReCreateIfNeed<T>(ref T[] a, int len) {
            if (a == null || a.Length != len) {
                a = new T[len];
                return true;
            }
            return false;
        }
        
        protected static ushort SafeGet(DepthBuffer depth, int x, int y) {
            if (x < 0 || x >= depth.width
                || y < 0 || y >= depth.height)
                return INVALID_DEPTH;
            return depth.data[depth.GetIFrom(x, y)];
        }

        protected static ushort SafeGet(DepthBuffer depth, Vector2Int xy) {
            return SafeGet(depth, xy.x, xy.y);
        }
    }
}