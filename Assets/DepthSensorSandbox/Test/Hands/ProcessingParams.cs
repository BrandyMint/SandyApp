using UnityEngine;

namespace DepthSensorSandbox.Test.Hands {
    public class ProcessingParams : MonoBehaviour {
        [SerializeField] private bool FixHoles = true;
        [SerializeField] private bool NoiseFilter = true;
        [SerializeField] private bool Hands = true; 
        [SerializeField] private float Exposition = 0.99f;
        [SerializeField] private ushort MaxError = 10;
        [SerializeField] private ushort MaxErrorAura = 10;
        [SerializeField] private ushort MinDistanceAtBorder = 100;
        
        private void Start() {
            FixHoles = DepthSensorSandboxProcessor.Instance.FixHoles.Active;
            NoiseFilter = DepthSensorSandboxProcessor.Instance.NoiseFilter.Active;
            Hands = DepthSensorSandboxProcessor.Instance.Hands.Active;
            
            Exposition = DepthSensorSandboxProcessor.Instance.Hands.Exposition;
            MaxError = DepthSensorSandboxProcessor.Instance.Hands.MaxError;
            MaxErrorAura = DepthSensorSandboxProcessor.Instance.Hands.MaxErrorAura;
            MinDistanceAtBorder = DepthSensorSandboxProcessor.Instance.Hands.MinDistanceAtBorder;
        }
        
        private void Update() {
            DepthSensorSandboxProcessor.Instance.FixHoles.Active = FixHoles;
            DepthSensorSandboxProcessor.Instance.NoiseFilter.Active = NoiseFilter;
            DepthSensorSandboxProcessor.Instance.Hands.Active = Hands;
            
            DepthSensorSandboxProcessor.Instance.Hands.Exposition = Exposition;
            DepthSensorSandboxProcessor.Instance.Hands.MaxError = MaxError;
            DepthSensorSandboxProcessor.Instance.Hands.MaxErrorAura = MaxErrorAura;
            DepthSensorSandboxProcessor.Instance.Hands.MinDistanceAtBorder = MinDistanceAtBorder;
        }
    }
}