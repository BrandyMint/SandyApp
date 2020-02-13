using Utilities.OpenCVSharpUnity;

namespace DepthSensor.Recorder {
    public class RecordCalibrationResult : SerializableParams {
        public RecordCalibrationResult() {}
        public RecordCalibrationResult(string path) {
            _path = path;
        }

        public string DeviceName {
            get => Get(nameof(DeviceName), "Unknown");
            set => Set(nameof(DeviceName), value);
        }

        public CameraIntrinsicParams IntrinsicDepth {
            get => Get<CameraIntrinsicParams>(nameof(IntrinsicDepth));
            set => Set(nameof(IntrinsicDepth), value);
        }

        public CameraIntrinsicParams IntrinsicColor {
            get => Get<CameraIntrinsicParams>(nameof(IntrinsicColor));
            set => Set(nameof(IntrinsicColor), value);
        }
        
        public double[,] R {
            get => Get<double[,]>(nameof(R));
            set => Set(nameof(R), value);
        }
        
        public double[,] T {
            get => Get<double[,]>(nameof(T));
            set => Set(nameof(T), value);
        }
    }
}