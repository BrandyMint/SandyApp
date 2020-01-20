using System;
using System.Linq;

namespace DepthSensor.Sensor {
    public abstract class AbstractSensor : ISensor, IDisposable {
        public bool Available { get; protected set; } = true;
        public event Action<ISensor> OnNewFrame;
        public event Action<ISensor> OnNewFrameBackground;

        public bool AnySubscribedToNewFrames => AnySubscribedTo(null, false, OnNewFrame, OnNewFrameBackground);

        public bool AnySubscribedToNewFramesExcept(params Type[] types) {
            return AnySubscribedTo(types, true, OnNewFrame, OnNewFrameBackground);
        }
        
        public bool AnySubscribedToNewFramesFrom(params Type[] types) {
            return AnySubscribedTo(types, false, OnNewFrame, OnNewFrameBackground);
        }

        public bool Active {
            get { return _active; }
            set { if (Available && _active != value) {
                _active = value;
                _onActiveChanged(this);
            }}
        }
        
        public int BuffersValid { get; protected set; }
        public int BuffersCount {
            get => _buffersCount;
            set {
                if (_buffersCount != value)
                    OnBuffersCountChanged(value);
                _buffersCount = value;
            }
        }

        protected Action<AbstractSensor> _onActiveChanged;
        private bool _active;
        protected int _buffersCount;
        
        public abstract void Dispose();

        protected abstract void OnBuffersCountChanged(int newCount);

        private static bool AnySubscribedTo(Type[] types, bool except, params Action<ISensor>[] actions) {
            if (actions == null)
                return false;
            return actions.Any(a => a != null && a.GetInvocationList().Any(d => {
                if (types == null)
                    return true;
                var dt = d.Method.ReflectedType;
                var hasTargetInTypes = types.Any(t => t.IsAssignableFrom(dt));
                if (except)
                    return !hasTargetInTypes;
                return hasTargetInTypes;
            }));
        }

        public class Internal {
            private readonly AbstractSensor _abstractSensor;

            protected internal Internal(AbstractSensor sensor) {
                _abstractSensor = sensor;
            }
            
            protected internal virtual void OnNewFrame() {
                _abstractSensor.OnNewFrame?.Invoke(_abstractSensor);
            }
            
            protected internal virtual void OnNewFrameBackground() {
                _abstractSensor.BuffersValid = Math.Min(_abstractSensor.BuffersValid + 1, _abstractSensor.BuffersCount);
                _abstractSensor.OnNewFrameBackground?.Invoke(_abstractSensor);
            }

            protected internal void SetOnActiveChanged(Action<AbstractSensor> onActiveChanged = null) {
                _abstractSensor._onActiveChanged = onActiveChanged;
            }
        }
    }
}