using UnityEngine;

namespace DepthSensor.Buffer {
    public class TextureBuffer<T> : Buffer2D<T>, ITextureBuffer where T : struct {
        public readonly Texture2D texture;
        
        public TextureBuffer(int width, int height, TextureFormat format) : 
            base(width, height, false) 
        {
            texture = new Texture2D(width, height, format, false);
            data = texture.GetRawTextureData<T>();
        }

        protected internal override object[] GetArgsForCreateSome() {
            return new object[] {width, height, texture.format};
        }

        public override void Dispose() {
            base.Dispose();
            Object.Destroy(texture);
        }

        public virtual void UpdateTexture() {
            texture.Apply(false);
        }
    }
}