using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        private float? _steering = null;
        private float? _accel = null;
        private float? _footbrake = null;
        private float? _handbrake = null;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
        }

        private void FixedUpdate()
        {
            // pass the input to the car!
            float h = _steering ?? Input.GetAxis("Horizontal");
            float v = _accel ?? Input.GetAxis("Vertical");
            float foot = _footbrake ?? v;
#if !MOBILE_INPUT
            float handbrake = _handbrake ?? Input.GetAxis("Jump");
            m_Car.Move(h, v, foot, handbrake);
#else
            m_Car.Move(h, v, foot, _handbrake ?? 0f);
#endif
        }

        public void OverrideSteering(float? steering) {
            _steering = steering;
        }
        
        public void OverrideAccel(float? accel) {
            _accel = accel;
        }
        
        public void OverrideFootbrake(float? foot) {
            _footbrake = foot;
        }

        public void OverrideHandbrake(float? handbrake) {
            _handbrake = handbrake;
        }
    }
}
