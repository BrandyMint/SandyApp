using System.Collections.Generic;
using DepthSensor.Buffer;

namespace DepthSensorSandbox.Processing {
    public interface IInitProcessing {
        void PrepareInitProcess();
        bool InitProcess(DepthBuffer rawBuffer, DepthBuffer outBuffer, DepthBuffer prevBuffer);
        IEnumerable<DepthBuffer> GetMapsForFixHoles();
    }
}