using UnityEngine;

namespace DepthSensor.Stream {
    public class TextureStream<T> : Stream<T> where T : struct {
        public readonly Texture2D texture;
        public bool AutoApplyTexture = false;
        
        private readonly Texture2D _internalTexture;

        public TextureStream(int width, int height, TextureFormat format, bool autoApply = false) : 
            base(width, height, false) 
        {
            if (format == TextureFormat.YUY2) {
                _internalTexture = new Texture2D(width, height, TextureFormat.RG16, false);
                data = _internalTexture.GetRawTextureData<T>();
                texture= Texture2D.CreateExternalTexture(width, height, TextureFormat.YUY2,
                    false, true,
                    _internalTexture.GetNativeTexturePtr()
                );
            } else {
                _internalTexture = texture = new Texture2D(width, height, format, false);
                data = texture.GetRawTextureData<T>();
            }
            AutoApplyTexture = autoApply;
        }

        public TextureStream(bool available) : this(1, 1, TextureFormat.RGB24) {
            Available = available;
        }

        public override void Dispose() {
            Object.Destroy(_internalTexture);
            Object.Destroy(texture);
        }

        public virtual void ManualApplyTexture() {
            if (!AutoApplyTexture)
                _internalTexture.Apply(false);
        }

        public new class Internal : Stream<T>.Internal {
            private readonly TextureStream<T> _stream;
            
            protected internal Internal(TextureStream<T> stream) : base(stream) {
                _stream = stream;
            }

            protected internal override void OnNewFrame() {
                if (_stream.AutoApplyTexture)
                    _stream._internalTexture.Apply(false);
                base.OnNewFrame();
            }
        }
    }
}