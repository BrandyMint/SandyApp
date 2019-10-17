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
            get => Get(nameof(Position), Vector3.zero);
            set => Set(nameof(Position), value);
        }

        [JsonConverter(typeof(JsonPublicFieldsConverter))]
        public Quaternion Rotation {
            get => Get(nameof(Rotation), Quaternion.identity);
            set => Set(nameof(Rotation), value);
        }

        public float Fov {
            get => Get(nameof(Fov), 60f);
            set => Set(nameof(Fov), value);
        }
        
        public float SensorSwitchingViewTimer {
            get => Get(nameof(SensorSwitchingViewTimer), 0.5f);
            set => Set(nameof(SensorSwitchingViewTimer), value);
        }
    }
}