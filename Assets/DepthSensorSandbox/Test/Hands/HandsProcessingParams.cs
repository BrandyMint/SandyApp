using UnityEngine;

namespace DepthSensorSandbox.Test.Hands {
    public class HandsProcessingParams : MonoBehaviour {
        
        [SerializeField] private float Exposition = 0.99f;
        [SerializeField] private ushort MaxError = 10;
        [SerializeField] public ushort MinDistanceAtBorder = 100;
        
        private void Start() {
            Exposition = DepthSensorSandboxProcessor.Instance.Hands.Exposition;
            MaxError = DepthSensorSandboxProcessor.Instance.Hands.MaxError;
            MinDistanceAtBorder = DepthSensorSandboxProcessor.Instance.Hands.MinDistanceAtBorder;
        }
        
        private void Update() {
            DepthSensorSandboxProcessor.Instance.Hands.Exposition = Exposition;
            DepthSensorSandboxProcessor.Instance.Hands.MaxError = MaxError;
            DepthSensorSandboxProcessor.Instance.Hands.MinDistanceAtBorder = MinDistanceAtBorder;
        }
    }
}