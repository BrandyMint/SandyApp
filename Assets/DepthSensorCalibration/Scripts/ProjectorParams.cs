using UnityEngine;

namespace DepthSensorCalibration {
    public class ProjectorParams : SerializableParams {
        public float Distance {
            get => Get(nameof(Distance), 1.2f);
            set => Set(nameof(Distance), value);
        }
        public float Diagonal {
            get => Get(nameof(Diagonal), 0.7f);
            set => Set(nameof(Diagonal), value);
        }
        public float Width {
            get => Get(nameof(Width), Display.displays.Length > 1 ? Display.displays[1].renderingWidth : 800f);
            set => Set(nameof(Width), value);
        }
        
        public float Height {
            get => Get(nameof(Height), Display.displays.Length > 1 ? Display.displays[1].renderingHeight : 600f);
            set => Set(nameof(Height), value);
        }
    }
}