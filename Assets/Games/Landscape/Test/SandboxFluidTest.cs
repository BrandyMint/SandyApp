using DepthSensorSandbox.Visualisation;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Landscape.Test {
    public class SandboxFluidTest : SandboxFluid {
        private static readonly int _INSTRUMENT = Shader.PropertyToID("_Instrument");
        private static readonly int _INSTRUMENT_TYPE = Shader.PropertyToID("_InstrumentType");
        
        [SerializeField] private Button _btnResetFluid;
        [SerializeField] private float _InstrumentSize = 25;
        [SerializeField] private float _InstrumentStrength = 1;

        protected override void Awake() {
            base.Awake();
            _btnResetFluid.onClick.AddListener(ClearFluidFlows);
        }

        protected override void OnCalibrationChange() {
        }

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
            _material.SetVector(_INSTRUMENT, instrument);
            _material.SetInt(_INSTRUMENT_TYPE, type);
        }
    }
}