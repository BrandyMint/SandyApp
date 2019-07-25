using Games.Landscape.DepthSensorSandbox;

public static partial class Prefs {
    public static LandscapeParams Landscape = new LandscapeParams();
}

namespace Games.Landscape {
    

    namespace DepthSensorSandbox {
        public class LandscapeParams : SerializableParams {
            public float DepthSeaBottom {
                get => Get(nameof(DepthSeaBottom), -0.1f);
                set => Set(nameof(DepthSeaBottom), value);
            }
            
            public float DepthSea {
                get => Get(nameof(DepthSea), 0f);
                set => Set(nameof(DepthSea), value);
            }
            
            public float DepthGround {
                get => Get(nameof(DepthGround), 0.02f);
                set => Set(nameof(DepthGround), value);
            }
            
            public float DepthMountains {
                get => Get(nameof(DepthMountains), 0.05f);
                set => Set(nameof(DepthMountains), value);
            }
            
            public float DepthIce {
                get => Get(nameof(DepthIce), 0.05f);
                set => Set(nameof(DepthIce), value);
            }
            
            public float DetailsSize {
                get => Get(nameof(DetailsSize), 1f);
                set => Set(nameof(DetailsSize), value);
            }
        }
    }
}