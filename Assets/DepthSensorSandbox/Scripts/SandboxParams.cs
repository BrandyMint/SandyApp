using DepthSensorSandbox;
using Newtonsoft.Json;
using UnityEngine;
using Utilities;

public static partial class Prefs {
    public static SandboxParams Sandbox = new SandboxParams();
}

namespace DepthSensorSandbox {
    public class SandboxParams : SerializableParams {
        public float ZeroDepth {
            get => Get(nameof(ZeroDepth), 1.6f);
            set => Set(nameof(ZeroDepth), value);
        }
        
        public float OffsetMinDepth {
            get => Get(nameof(OffsetMinDepth), 0.2f);
            set => Set(nameof(OffsetMinDepth), value);
        }
        
        public float OffsetMaxDepth {
            get => Get(nameof(OffsetMaxDepth), 0.2f);
            set => Set(nameof(OffsetMaxDepth), value);
        }
        
        public float PercentToDepth(float p) {
            if (p < 0) {
                return Mathf.Lerp(ZeroDepth + OffsetMinDepth, ZeroDepth, 1 + p);
            } else {
                return Mathf.Lerp(ZeroDepth, ZeroDepth - OffsetMaxDepth, p);
            }
        }
    }
}