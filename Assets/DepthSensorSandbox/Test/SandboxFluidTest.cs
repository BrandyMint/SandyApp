using DepthSensorSandbox.Visualisation;
using UnityEngine;
using UnityEngine.UI;

namespace DepthSensorSandbox.Test {
    public class SandboxFluidTest : SandboxFluid {
        private const string _DYNAMIC_FLUID = "DYNAMIC_FLUID";
        private const string _DRAW_LANDSCAPE = "DRAW_LANDSCAPE";
        private static readonly int _INSTRUMENT = Shader.PropertyToID("_Instrument");
        private static readonly int _INSTRUMENT_TYPE = Shader.PropertyToID("_InstrumentType");
        private static readonly int _DEPTH_ZERO = Shader.PropertyToID("_DepthZero");
        private static readonly int _FLUX_ACCELERATION = Shader.PropertyToID("_FluxAcceleration");
        private static readonly int _FLUX_FADING = Shader.PropertyToID("_FluxFading");
        private static readonly int _CELL_HEIGHT = Shader.PropertyToID("_CellHeight");
        private static readonly int _CELL_AREA = Shader.PropertyToID("_CellArea");
        
        [SerializeField] private Button _btnResetFluid;
        [SerializeField] private float _InstrumentSize = 25;
        [SerializeField] private float _InstrumentStrength = 1;
        [SerializeField] private float _depthZero = 1.6f;
        [SerializeField] private float _fluxAcceleration = 9.8f;
        [SerializeField] private float _fluxFading = 0.1f;
        [SerializeField] private float _cellWidth = 1;
        [SerializeField] private float _cellHeight = 1;

        protected override void Awake() {
            base.Awake();
            _material.EnableKeyword(_DYNAMIC_FLUID);
            _material.EnableKeyword(_DRAW_LANDSCAPE);
            _btnResetFluid.onClick.AddListener(ClearFluidFlows);
        }

        protected override void OnCalibrationChange() { }

        protected override void Update() {
            base.Update();
            var instrument = new Vector4(
                Input.mousePosition.x, 
                Input.mousePosition.y, 
                _InstrumentSize,
                _InstrumentStrength
            );
            var type = 0;
            if (Input.GetMouseButton(0)) {
                type = 1;
            } else 
            if (Input.GetMouseButton(1)) {
                type = 2;
            }
            _matFluidCalc.SetVector(_INSTRUMENT, instrument);
            _matFluidCalc.SetInt(_INSTRUMENT_TYPE, type);
            _matFluidCalc.SetFloat(_FLUX_ACCELERATION, _fluxAcceleration);
            _matFluidCalc.SetFloat(_FLUX_FADING, _fluxFading);
            _matFluidCalc.SetFloat(_CELL_HEIGHT, _cellHeight);
            _matFluidCalc.SetFloat(_CELL_AREA, _cellWidth * _cellWidth);
            
            var props = _sandbox.PropertyBlock;
            props.SetFloat(_DEPTH_ZERO, _depthZero);
            _sandbox.PropertyBlock = props;
        }
    }
}