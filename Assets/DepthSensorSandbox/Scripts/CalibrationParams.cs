using DepthSensorSandbox;
using Newtonsoft.Json;
using UnityEngine;
using Utilities;

public static partial class Prefs {
    public static CalibrationParams Calibration = new CalibrationParams();
}

namespace DepthSensorSandbox {
    public class CalibrationParams : SerializableParams {
        [JsonConverter(typeof(JsonPublicFieldsConverter))]
        public Vector3 Position {
            get => Get(nameof(Position), new Vector3(0f, 0f, -0.6f));
            set => Set(nameof(Position), value);
        }

        [JsonConverter(typeof(JsonPublicFieldsConverter))]
        public Quaternion Rotation {
            get => Get(nameof(Rotation), Quaternion.identity);
            set => Set(nameof(Rotation), value);
        }

        public float Fov {
            get => Get(nameof(Fov), 26.4f);
            set => Set(nameof(Fov), value);
        }
        
        public float Oblique {
            get => Get(nameof(Oblique), 0f);
            set => Set(nameof(Oblique), value);
        }
        
        public float SensorSwitchingViewTimer {
            get => Get(nameof(SensorSwitchingViewTimer), 1f);
            set => Set(nameof(SensorSwitchingViewTimer), value);
        }
    }
}