using DepthSensor.Buffer;

namespace DepthSensor.Sensor {
    public class SensorColor : Sensor<ColorBuffer> {
        public SensorColor(ColorBuffer buffer) : base(buffer) { }
        public SensorColor(bool available) : base(available) { }
    }

    public class SensorDepth : Sensor<DepthBuffer> {
        public SensorDepth(DepthBuffer buffer) : base(buffer) { }
        public SensorDepth(bool available) : base(available) { }
    }
    
    public class SensorInfrared : Sensor<InfraredBuffer> {
        public SensorInfrared(InfraredBuffer buffer) : base(buffer) { }
        public SensorInfrared(bool available) : base(available) { }
    }

    public class SensorIndex : Sensor<IndexBuffer> {
        public SensorIndex(IndexBuffer buffer) : base(buffer) { }
        public SensorIndex(bool available) : base(available) { }
    }

    public class SensorMapDepthToCamera : Sensor<MapDepthToCameraBuffer> {
        public SensorMapDepthToCamera(MapDepthToCameraBuffer buffer) : base(buffer) { }
        public SensorMapDepthToCamera(bool available) : base(available) { }
    }
    
    public class SensorBody : Sensor<BodyBuffer> {
        public SensorBody(BodyBuffer buffer) : base(buffer) { }
        public SensorBody(bool available) : base(available) { }
    }
}