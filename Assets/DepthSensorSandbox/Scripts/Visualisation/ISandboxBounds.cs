using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    public interface ISandboxBounds {
        bool IsBoundsValid();
        Bounds GetBounds();
        void RequestUpdateBounds();
    }
}