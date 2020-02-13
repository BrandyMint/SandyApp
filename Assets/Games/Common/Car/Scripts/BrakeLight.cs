using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car
{
    public class BrakeLight : MonoBehaviour
    {
        public CarController car; // reference to the car controller, must be dragged in inspector
        public GameObject _brakeLightObj;

        private Renderer m_Renderer;
        private bool _enabled = true;


        private void Start()
        {
            m_Renderer = GetComponent<Renderer>();
            SetEnabled(false);
        }


        private void Update()
        {
            // enable the Renderer when the car is braking, disable it otherwise.
            SetEnabled(car.BrakeInput > 0f);
        }

        private void SetEnabled(bool enabled) {
            if (_enabled == enabled)
                return;
            _enabled = enabled;
            if (_brakeLightObj != null)
                _brakeLightObj.SetActive(enabled);
            else {
                m_Renderer.enabled = enabled;
            }
        }
    }
}
