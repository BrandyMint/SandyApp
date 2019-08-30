using DepthSensor.Buffer;

namespace DepthSensorSandbox.Processing {
    public abstract class ProcessingBase {
        public const ushort INVALID_DEPTH = 0;
        
        public bool OnlyRawBuffersIsInput = true;
        public bool Active = true;

        public void Process(DepthBuffer[] rawBuffers, DepthBuffer inOut) {
            if (Active) {
                ProcessInternal(rawBuffers, inOut);
            }
        }

        protected abstract void ProcessInternal(DepthBuffer[] rawBuffers, DepthBuffer inOut);

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
    }
}