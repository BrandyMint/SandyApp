using System;
using UnityEngine;

namespace DepthSensor.Recorder {
    [Serializable]
    public class StreamInfo {
        public int width;
        public int height;
        public int framesCount;
        public int fps;
        public TextureFormat textureFormat;
    }

    public class RecordManifest : SerializableParams {
        public RecordManifest(string path) {
            _path = path;
        }
        
        public string DeviceName {
            get => Get(nameof(DeviceName), "Unknown");
            set => Set(nameof(DeviceName), value);
        }

        public StreamInfo Depth {
            get => Get<StreamInfo>(nameof(Depth));
            set => Set(nameof(Depth), value);
        }

        public StreamInfo Infrared {
            get => Get<StreamInfo>(nameof(Infrared));
            set => Set(nameof(Infrared), value);
        }

        public StreamInfo Color {
            get => Get<StreamInfo>(nameof(Color));
            set => Set(nameof(Color), value);
        }
        
        public StreamInfo MapDepthToCamera {
            get => Get<StreamInfo>(nameof(MapDepthToCamera));
            set => Set(nameof(MapDepthToCamera), value);
        }
    }
}