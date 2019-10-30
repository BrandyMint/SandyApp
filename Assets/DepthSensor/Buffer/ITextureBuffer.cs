using UnityEngine;

namespace DepthSensor.Buffer {
    public interface ITextureBuffer : IBuffer {
        void UpdateTexture();
        Texture GetTexture();
    }
}