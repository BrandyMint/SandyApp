using Games.Landscape.DepthSensorSandbox;

public static partial class Prefs {
    public static LandscapeParams Landscape = new LandscapeParams();
}

namespace Games.Landscape {
    

    namespace DepthSensorSandbox {
        public class LandscapeParams : SerializableParams {
            public float DepthSeaBottom {
                get => Get(nameof(DepthSeaBottom), -10f);
                set => Set(nameof(DepthSeaBottom), value);
            }
            
            public float DepthSea {
                get => Get(nameof(DepthSea), 0f);
                set => Set(nameof(DepthSea), value);
            }
            
            public float DepthGround {
                get => Get(nameof(DepthGround), 2f);
                set => Set(nameof(DepthGround), value);
            }
            
            public float DepthMountains {
                get => Get(nameof(DepthMountains), 5f);
                set => Set(nameof(DepthMountains), value);
            }
        }
    }
}