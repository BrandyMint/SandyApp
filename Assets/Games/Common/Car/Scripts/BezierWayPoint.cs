using System;
using BezierSolution;
using Unity.Mathematics;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car {
    public class BezierWayPoint : MonoBehaviour {
        public BezierSpline spline;
        [SerializeField] private Transform _wayPoint;
        [SerializeField] private float _accuracy = 200f;

        private Vector3 _initialDist; 

        private void Start() {
            _initialDist = _wayPoint.localPosition;
        }

        private void Update() {
            if (spline != null) {
                var dist = transform.TransformVector(_initialDist).magnitude;
                spline.FindNearestPointTo(transform.position, out var t, _accuracy);
                _wayPoint.position = spline.MoveAlongSpline(ref t, dist);
                _wayPoint.rotation = quaternion.LookRotation(spline.GetTangent(t), transform.up);
            }
        }
    }
}