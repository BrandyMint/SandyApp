using DepthSensorSandbox;
using UnityEngine;

namespace Games.Common {
    public class HandsProcessingEnabler : MonoBehaviour {
        [SerializeField] private bool _enable = true;
        
        private void Start() {
            if (DepthSensorSandboxProcessor.Instance != null)
                DepthSensorSandboxProcessor.Instance.HandsProcessingSwitch(_enable);
        }
    }
}