using DepthSensorSandbox.Visualisation;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Landscape.Test {
    public class SandboxFluidTest : SandboxFluid {
        [SerializeField] private Button _btnResetFluid;

        protected override void Awake() {
            base.Awake();
            _btnResetFluid.onClick.AddListener(ClearFluidFlows);
        }

        protected override void OnCalibrationChange() {
        }
    }
}