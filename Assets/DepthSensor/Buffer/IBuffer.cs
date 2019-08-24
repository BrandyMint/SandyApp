using System;

namespace DepthSensor.Buffer {
    public interface IBuffer : IDisposable {
        T CreateSome<T>() where T : IBuffer;
        
        bool Lock(int milliseconds = -1);

        void Unlock();

        void SafeUnlock();
    }
}