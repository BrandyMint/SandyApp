using System;

namespace DepthSensor.Buffer {
    public interface IBuffer : IDisposable {
        AbstractBuffer CreateSome();
        
        bool Lock(int milliseconds = -1);

        void Unlock();

        void SafeUnlock();
    }
}