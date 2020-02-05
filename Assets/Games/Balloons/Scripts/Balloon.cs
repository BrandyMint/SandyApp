using System;
using Games.Common;
using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Balloons {
    public class Balloon : InteractableModel {
        [SerializeField] private GameObject _string;
        public static event Action<Balloon, Collision> OnCollisionEntered;

        public override void Bang(bool isRight) {
            GetComponent<ConstantForce>().enabled = false;
            var body = GetComponent<Rigidbody>();
            body.velocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.angularVelocity = Vector3.zero;
            
            var module = _rightBang.main; 
            module.startColor = GetComponentInChildren<RandomColorRenderer>().GetColor();
            
            base.Bang(isRight);
        }

        private void OnCollisionEnter(Collision other) {
            OnCollisionEntered?.Invoke(this, other);
        }

        public GameObject String => _string;

        public float FullMass;
    }
}