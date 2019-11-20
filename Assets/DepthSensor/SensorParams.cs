using System;
using DepthSensor;

public static partial class Prefs {
    public static SensorParams Sensor = new SensorParams();
}

namespace DepthSensor {
    [Serializable]
    public struct StreamParams {
        public int width;
        public int height;
        public int fps;
        public bool use;

        public StreamParams(int width, int height, int fps, bool use = false) {
            this.width = width;
            this.height = height;
            this.fps = fps;
            this.use = use;
        }
        
        public static readonly StreamParams Default = new StreamParams(640, 480, 30);
    }
    
    public class SensorParams : SerializableParams {
        public StreamParams Depth {
            get => Get(nameof(Depth), StreamParams.Default);
            set => Set(nameof(Depth), value);
        }
        
        public StreamParams Color {
            get => Get(nameof(Color), StreamParams.Default);
            set => Set(nameof(Color), value);
        }
        
        public StreamParams IR {
            get => Get(nameof(IR), StreamParams.Default);
            set => Set(nameof(IR), value);
        }
    }
}