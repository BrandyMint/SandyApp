﻿using UnityEngine;

namespace DepthSensor.Stream {
    public class TextureStream<T> : Stream<T> where T : struct {
        public readonly Texture2D texture;
        public bool AutoApplyTexture = false;
        
        public TextureStream(int width, int height, TextureFormat format, bool autoApply = false) : 
            base(width, height, false) 
        {
            texture = new Texture2D(width, height, format, false);
            data = texture.GetRawTextureData<T>();
            AutoApplyTexture = autoApply;
        }

        public TextureStream(bool available) : this(1, 1, TextureFormat.RGB24) {
            Available = available;
        }

        public override void Dispose() {
            Object.Destroy(texture);
        }

        public virtual void ManualApplyTexture() {
            if (!AutoApplyTexture)
                texture.Apply(false);
        } 

        public new class Internal : Stream<T>.Internal {
            private readonly TextureStream<T> _stream;
            
            protected internal Internal(TextureStream<T> stream) : base(stream) {
                _stream = stream;
            }

            protected internal override void OnNewFrame() {
                if (_stream.AutoApplyTexture)
                    _stream.texture.Apply(false);
                base.OnNewFrame();
            }
        }
    }
}