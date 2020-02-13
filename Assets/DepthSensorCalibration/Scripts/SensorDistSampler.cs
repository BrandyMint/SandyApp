using System;
using DepthSensor.Buffer;
using DepthSensorSandbox;
using DepthSensorSandbox.Visualisation;
using Unity.Mathematics;
using UnityEngine;

namespace DepthSensorCalibration {
    public class SensorDistSampler : MonoBehaviour {
        public int SamplesCount = 15;
        public float AreaSize = 0.05f;

        public event Action<Vector3[]> OnSampleAreaPoints; 
        public event Action<float> OnDistReceive;

        private int _samplesLeft;
        private float _samplesSum;

        private void Start() {
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }

        public void StartSampling() {
            _samplesSum = 0f;
            _samplesLeft = SamplesCount;
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer map) {
            if (_samplesLeft > 0) {
                GetArea(depth, out var center, out var halfSize);
                InvokeSampleAreaPoints(depth, map, center, halfSize);
                _samplesSum += Sample(depth, center, halfSize);
                --_samplesLeft;
                if (_samplesLeft == 0) {
                    OnDistReceive?.Invoke(_samplesSum / SamplesCount);
                }
            }
        }

        private float Sample(DepthBuffer depth, int2 center, int halfSize) {
            int count = 0;
            float sum = 0f;
            for (int x = -halfSize; x <= halfSize; ++x) {
                for (int y = -halfSize; y <= halfSize; ++y) {
                    var i = depth.GetIFrom(center.x + x, center.y + y);
                    var d = depth.data[i];
                    if (d > 0) {
                        sum += d / 1000f;
                        ++count;
                    }
                }
            }
            return sum / count;
        }

        private void GetArea(DepthBuffer depth, out int2 center, out int halfSize) {
            center = new int2(depth.width, depth.height) / 2;
            halfSize = (int) (Mathf.Min(depth.width, depth.height) * AreaSize / 2);
        }
        
        private readonly Vector3[] _pointsCache = new Vector3[4];
        private readonly int2[] _bordersCache = {
            new int2(-1, -1), new int2(-1, 1), 
            new int2(1, 1), new int2(1, -1)
        };
        private void InvokeSampleAreaPoints(DepthBuffer depth, MapDepthToCameraBuffer map, int2 center, int halfSize) {
            for (int k = 0; k < _bordersCache.Length; ++k) {
                var p = center + _bordersCache[k] * halfSize;
                var i = depth.GetIFrom(p.x, p.y);
                _pointsCache[k] = SandboxMesh.PointDepthToVector3(depth, map, i);
            }
            OnSampleAreaPoints?.Invoke(_pointsCache);
        }
    }
}