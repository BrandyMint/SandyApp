using DepthSensor.Buffer;
using UnityEngine;

namespace DepthSensorSandbox.Processing {
    public class DepthToColorBuffer : TextureBuffer<Vector2> {
        public DepthToColorBuffer(int width, int height) : base(width, height, TextureFormat.RGFloat) { }
        
        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height};
        }
    }
}