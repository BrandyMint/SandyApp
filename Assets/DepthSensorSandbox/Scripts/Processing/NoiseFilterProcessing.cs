using System.Threading.Tasks;
using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class NoiseFilterProcessing : ProcessingBase {
        public float Smooth = 0.9f;
        public ushort MaxError = 50;

        private DepthBuffer[] _rawBuffers;
        private DepthBuffer _inOut;
        
        protected override void ProcessInternal(DepthBuffer[] rawBuffers, DepthBuffer inOut) {
            _rawBuffers = rawBuffers;
            _inOut = inOut;
            Parallel.For(0, inOut.data.Length, FilterBody);
        }

        private void FilterBody(int i) {
            var depth = _rawBuffers[0];
            var actualVal = depth.data[i];
            if (actualVal == INVALID_DEPTH) {
                _inOut.data[i] = INVALID_DEPTH;
                return;
            }
            
            var prevVal = _inOut.data[i];
            var error = Mathf.Abs(actualVal - prevVal);
            if (error > MaxError || prevVal == INVALID_DEPTH) {
                _inOut.data[i] = actualVal;
            } else {
                var k = Smooth * Mathf.Sqrt((float)(MaxError - error) / MaxError);
                _inOut.data[i] = (ushort) Mathf.Lerp(actualVal, prevVal, k);
            }
        }
    }
}