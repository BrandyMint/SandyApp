using DepthSensorCalibration;

public static partial class Prefs {
    public static ProjectorParams Projector = new ProjectorParams();
}

namespace DepthSensorCalibration {
    public class ProjectorParams : SerializableParams {
        public float DistanceToSensor {
            get => Get(nameof(DistanceToSensor), -1f);
            set => Set(nameof(DistanceToSensor), value);
        }
        
        public float Width {
            get => Get(nameof(Width), 1.333f);
            set => Set(nameof(Width), value);
        }
        
        public float Height {
            get => Get(nameof(Height), 0.750f);
            set => Set(nameof(Height), value);
        }
    }
}