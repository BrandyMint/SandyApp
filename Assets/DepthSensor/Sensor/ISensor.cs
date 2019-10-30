using System;
using DepthSensor.Buffer;

namespace DepthSensor.Sensor {
    public interface ISensor {
        bool Available { get; }
        event Action<ISensor> OnNewFrame;
        event Action<ISensor> OnNewFrameBackground;
        bool Active { get; set; }
        int BuffersValid { get; }
        int BuffersCount { get; set; }
    }
    
    public interface ISensor<out T> : ISensor where T : IBuffer {
        T Get(int i);
        T GetNewest();
        T GetOldest();
        T GetNewestAndLock(int milliseconds = -1);
    }
}