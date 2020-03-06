using UnityEngine;

namespace DepthSensorSandbox.Test.Hands {
    public class ProcessingParams : MonoBehaviour {
        [SerializeField] private bool FixHoles = true;
        [SerializeField] private bool NoiseFilter = true;
        [SerializeField] private bool Hands = true; 
        [SerializeField] private float Exposition;
        [SerializeField] private float MaxErrorFactor;
        [SerializeField] private float MaxErrorAuraFactor;
        [SerializeField] private ushort MinDistanceAtBorder;
        [SerializeField, Range(0f, 1f)] private float MaxBiasDot;
        [SerializeField] private int WavesCountErrorAuraExtend;
        
        private void Start() {
            DepthSensorSandboxProcessor.Instance.HandsProcessingSwitch(true);
            FixHoles = DepthSensorSandboxProcessor.Instance.FixHoles.Active;
            NoiseFilter = DepthSensorSandboxProcessor.Instance.NoiseFilter.Active;
            Hands = DepthSensorSandboxProcessor.Instance.Hands.Active;
            
            Exposition = DepthSensorSandboxProcessor.Instance.Hands.Exposition;
            MaxErrorFactor = DepthSensorSandboxProcessor.Instance.Hands.MaxErrorFactor;
            MaxErrorAuraFactor = DepthSensorSandboxProcessor.Instance.Hands.MaxErrorAuraFactor;
            MinDistanceAtBorder = DepthSensorSandboxProcessor.Instance.Hands.MinDistanceAtBorder;
            MaxBiasDot = DepthSensorSandboxProcessor.Instance.Hands.MaxBiasDot;
            WavesCountErrorAuraExtend = DepthSensorSandboxProcessor.Instance.Hands.WavesCountErrorAuraExtend;
        }
        
        private void Update() {
            DepthSensorSandboxProcessor.Instance.FixHoles.Active = FixHoles;
            DepthSensorSandboxProcessor.Instance.NoiseFilter.Active = NoiseFilter;
            DepthSensorSandboxProcessor.Instance.Hands.Active = Hands;
            
            DepthSensorSandboxProcessor.Instance.Hands.Exposition = Exposition;
            DepthSensorSandboxProcessor.Instance.Hands.MaxErrorFactor = MaxErrorFactor;
            DepthSensorSandboxProcessor.Instance.Hands.MaxErrorAuraFactor = MaxErrorAuraFactor;
            DepthSensorSandboxProcessor.Instance.Hands.MinDistanceAtBorder = MinDistanceAtBorder;
            DepthSensorSandboxProcessor.Instance.Hands.MaxBiasDot = MaxBiasDot;
            DepthSensorSandboxProcessor.Instance.Hands.WavesCountErrorAuraExtend = WavesCountErrorAuraExtend;
        }
    }
}