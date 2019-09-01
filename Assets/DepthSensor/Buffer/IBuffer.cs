using System;

namespace DepthSensor.Buffer {
    public interface IBuffer : IDisposable {
        T CreateSome<T>() where T : IBuffer;
        
        T Copy<T>() where T : IBuffer;
        
        void Clear();
        
        bool Lock(int milliseconds = -1);

        void Unlock();

        void SafeUnlock();
    }
}