using System;
using DepthSensor.Buffer;

namespace DepthSensor.Sensor {
    public interface ISensor {
        bool Available { get; }
        event Action<ISensor> OnNewFrame;
        event Action<ISensor> OnNewFrameBackground;
        bool AnySubscribedToNewFrames { get; }
        bool AnySubscribedToNewFramesExcept(params Type[] types);
        bool AnySubscribedToNewFramesFrom(params Type[] types);
        bool Active { get; set; }
        int BuffersValid { get; }
        int BuffersCount { get; set; }
        int FPS { get; }
    }
    
    public interface ISensor<out T> : ISensor where T : IBuffer {
        T Get(int i);
        T GetNewest();
        T GetOldest();
    }
}