using System;

namespace DepthSensor.Stream {
    public abstract class AbstractStream : IDisposable {
        public bool Available { get; protected set; } = true;
        public event Action<AbstractStream> OnNewFrame;
        public event Action<AbstractStream> OnNewFrameBackground;
        
        public bool Active {
            get { return _active; }
            set { if (Available && _active != value) {
                _active = value;
                _onActiveChanged(this);
            }}
        }

        protected Action<AbstractStream> _onActiveChanged;
        private bool _active;

        public abstract void Dispose();

        public class Internal {
            private readonly AbstractStream _abstractStream;

            protected internal Internal(AbstractStream stream) {
                _abstractStream = stream;
            }
            
            protected internal virtual void OnNewFrame() {
                if (_abstractStream.OnNewFrame != null) _abstractStream.OnNewFrame(_abstractStream);
            }
            
            protected internal virtual void OnNewFrameBackground() {
                if (_abstractStream.OnNewFrameBackground != null) _abstractStream.OnNewFrameBackground(_abstractStream);
            }

            protected internal void SetOnActiveChanged(Action<AbstractStream> onActiveChanged = null) {
                _abstractStream._onActiveChanged = onActiveChanged;
            }
        }
    }
}