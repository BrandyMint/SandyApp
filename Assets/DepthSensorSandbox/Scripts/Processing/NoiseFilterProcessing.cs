using System.Threading.Tasks;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class NoiseFilterProcessing : ProcessingBase {
        public float Smooth = 0.9f;
        public ushort MaxError = 50;
        
        protected override void ProcessInternal() {
            Parallel.For(0, _out.data.Length, FilterBody);
        }

        private void FilterBody(int i) {
            var actualVal = _rawBuffer.data[i];
            if (actualVal == INVALID_DEPTH) {
                _out.data[i] = INVALID_DEPTH;
                return;
            }
            
            var prevVal = _prev.data[i];
            var error = Mathf.Abs(actualVal - prevVal);
            if (error > MaxError || prevVal == INVALID_DEPTH) {
                _out.data[i] = actualVal;
            } else {
                var k = Smooth * Mathf.Sqrt((float)(MaxError - error) / MaxError);
                _out.data[i] = (ushort) Mathf.Lerp(actualVal, prevVal, k);
            }
        }
    }
}