using System;
using DepthSensor.Buffer;
using DepthSensorSandbox.Processing;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(MeshCollider))]
    public class SandboxCollider : MonoBehaviour, ISandboxBounds {
        [SerializeField] private int _maxHeight = 64;
        [SerializeField] private int _updateOnFrame = 3;
        
        private Mesh _mesh;
        private Vector3[] _vert;
        private int[] _triangles;
        private bool _needUpdateBounds = true;
        private bool _isBoundsValid;
        private MeshCollider _collider;
        private int _frameBG;
        private int _frameMain;
        private Sampler _s = Sampler.Create();
        private Sampler _sFull = Sampler.Create();
        private Rect _cropping = Sampler.FULL_CROPPING;
        private bool _needUpdateCropping;
        private bool _needUpdateIndexes;

        private void Awake() {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
                _mesh = meshFilter.mesh;
            _collider = GetComponent<MeshCollider>();
            if (_mesh == null || _mesh.vertexCount == 0) {
                _mesh = new Mesh {name = "collider"};
                _mesh.MarkDynamic();
            }
            _collider.sharedMesh = _mesh;
            _frameBG = _updateOnFrame;
            _frameMain = _updateOnFrame / 2;
        }

        private void Start() {
            if (DepthSensorSandboxProcessor.Instance) {
                OnCroppingChanged(DepthSensorSandboxProcessor.Instance.GetCroppingExtended());
            }
            DepthSensorSandboxProcessor.OnDepthDataBackground += OnDepthData;
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
            DepthSensorSandboxProcessor.OnCroppingChanged += OnCroppingChanged;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthData;
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
            DepthSensorSandboxProcessor.OnCroppingChanged -= OnCroppingChanged;
        }
        
        private void OnCroppingChanged(Rect rect) {
            _cropping = DepthSensorSandboxProcessor.Instance.GetCroppingExtended();
            _needUpdateCropping = true;
        }
        
        public bool ReInitMeshIfNeed(int width, int height) {
            _s.SetDimens(width, height);
            var needRecalcIndexes = _needUpdateCropping;
            if (_needUpdateCropping) {
                _sFull.SetCropping01(_cropping);
                _s.SetCropping01(_cropping);
                _needUpdateCropping = false;
            }
            var updated = SandboxMesh.ReInitMeshIfNeed(_s, ref _vert, ref _triangles, needRecalcIndexes);
            _needUpdateIndexes |= updated;
            return updated;
        }

        private void OnDepthData(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (!IsExecuteFrame(ref _frameBG)) return;
            
            _sFull.SetDimens(depth.width, depth.height);
            _scale = Mathf.RoundToInt((float) depth.height / Mathf.Min(depth.height, _maxHeight));
            int height = depth.height / _scale;
            int width = depth.width / _scale;
            ReInitMeshIfNeed(width, height);
            _scale2 = Mathf.RoundToInt((float) _scale / 2);
            _currDepth = depth;
            _currMapToCamera = mapToCamera;
            _s.EachParallelHorizontal(UpdateMeshBody);
        }

        private int _scale;
        private int _scale2;
        private DepthBuffer _currDepth;
        private MapDepthToCameraBuffer _currMapToCamera;
        private void UpdateMeshBody(int i) {
            var r = _sFull.Rect;
            var pFull = _sFull.GetXYiConverted(_s, i);
            pFull.y = Mathf.Clamp(pFull.y, r.yMin, r.yMax);
            var iFull = _sFull.GetIFrom(pFull.x, pFull.y);
            
            var d = Vector3.zero;
            var s1 = Mathf.Clamp(-_scale2, r.xMin - pFull.x, 0);
            var s2 = Mathf.Clamp(_scale2,  0, r.xMax - pFull.x);
            var count = s2 - s1;
            if (count < _scale2) {
                var d2 = (_scale2 - count) / 2;
                s1 -= d2;
                s2 += d2;
                count = s2 - s1;
            }

            for (var xd = s1; xd < s2; ++xd) {
                d += SandboxMesh.PointDepthToVector3(_currDepth, _currMapToCamera, iFull + xd);
            }
            
            var j = _s.GetIInRect(i);
            _vert[j] = d / count;
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (!IsExecuteFrame(ref _frameMain)) return;
            
            if (_vert != null && _triangles != null) {
                if (_needUpdateIndexes)
                    _mesh.Clear();
                _mesh.vertices = _vert;
                if (_needUpdateIndexes) {
                    _needUpdateIndexes = false;
                    _mesh.indexFormat = _vert.Length > UInt16.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    _mesh.triangles = _triangles;
                    _needUpdateBounds = true;
                }

                if (_needUpdateBounds) {
                    _needUpdateBounds = false;
                    _mesh.RecalculateBounds();
                    _isBoundsValid = true;
                }
                
                _collider.sharedMesh = _mesh;
            }
        }

        private bool IsExecuteFrame(ref int frame) {
            frame %= _updateOnFrame;
            var exec = frame == 0;
            ++frame;
            return exec;
        }
        
        public Bounds GetBounds() {
            return _mesh.bounds;
        }

        public bool IsBoundsValid() {
            return _isBoundsValid;
        }

        public void RequestUpdateBounds() {
            _isBoundsValid = false;
            _needUpdateBounds = true;
        }
    }
}