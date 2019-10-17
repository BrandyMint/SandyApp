using Games.Landscape.DepthSensorSandbox;

public static partial class Prefs {
    public static LandscapeParams Landscape = new LandscapeParams();
}

namespace Games.Landscape {
    

    namespace DepthSensorSandbox {
        public class LandscapeParams : SerializableParams {
            public float DepthSeaBottom {
                get => Get(nameof(DepthSeaBottom), -1f);
                set => Set(nameof(DepthSeaBottom), value);
            }
            
            public float DepthSea {
                get => Get(nameof(DepthSea), 0f);
                set => Set(nameof(DepthSea), value);
            }
            
            public float DepthGround {
                get => Get(nameof(DepthGround), 0.1f);
                set => Set(nameof(DepthGround), value);
            }
            
            public float DepthMountains {
                get => Get(nameof(DepthMountains), 0.5f);
                set => Set(nameof(DepthMountains), value);
            }
            
            public float DepthIce {
                get => Get(nameof(DepthIce), 1f);
                set => Set(nameof(DepthIce), value);
            }
            
            public float DetailsSize {
                get => Get(nameof(DetailsSize), 1f);
                set => Set(nameof(DetailsSize), value);
            }
            
            public bool EnableWaterSimulation {
                get => Get(nameof(EnableWaterSimulation), true);
                set => Set(nameof(EnableWaterSimulation), value);
            }
            
            public float FluidResolution {
                get => Get(nameof(FluidResolution), 256f);
                set => Set(nameof(FluidResolution), value);
            }
            
            public float FluidCellSize {
                get => Get(nameof(FluidCellSize), 1f);
                set => Set(nameof(FluidCellSize), value);
            }
            
            public float FluidAcceleration {
                get => Get(nameof(FluidAcceleration), 9.8f);
                set => Set(nameof(FluidAcceleration), value);
            }
            
            public float FluidFading {
                get => Get(nameof(FluidFading), 0.1f);
                set => Set(nameof(FluidFading), value);
            }
        }
    }
}