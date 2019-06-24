using System;

namespace DepthSensor.Sensor {
    public abstract class AbstractSensor {
        public bool Available { get; protected set; } = true;
        
        public bool Active {
            get { return _active; }
            set { if (Available && _active != value) {
                _active = value;
                _onActiveChanged(this);
            }}
        }

        protected Action<AbstractSensor> _onActiveChanged;
        private bool _active;
    }
}