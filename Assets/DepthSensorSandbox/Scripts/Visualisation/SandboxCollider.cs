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
        [SerializeField] private float _croppingExtend = 0.1f;
        
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
        private bool _needRecalcIndexes;
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
                OnCroppingChanged(DepthSensorSandboxProcessor.Instance.GetCropping());
                UpdateCropping(_cropping);
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
            _cropping = rect;
            _needUpdateCropping = true;
        }
        
        private void UpdateCropping(Rect rect) {
            rect.max += Vector2.one * _croppingExtend;
            rect.min -= Vector2.one * _croppingExtend;
            _s.SetCropping01(rect);
            _needUpdateCropping = false;
            _needRecalcIndexes = true;
        }
        
        public bool ReInitMeshIfNeed(int width, int height) {
            _s.SetDimens(width, height);
            var updated = SandboxMesh.ReInitMeshIfNeed(_s, ref _vert, ref _triangles, _needRecalcIndexes);
            _needRecalcIndexes = false;
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
            var pFull = _s.GetXYiFrom(i) * _scale;
            var iFull = _sFull.GetIFrom(pFull.x, pFull.y);
            var d = Vector3.zero;
            var count = 0;
            for (var xd = -_scale2; xd < _scale2; ++xd) {
                var xx = pFull.x + xd;
                if (xx >= 0 && xx < _sFull.width) {
                    d += SandboxMesh.PointDepthToVector3(_currDepth, _currMapToCamera, iFull + xd);
                    ++count;
                }
            }
            
            var j = _s.GetIInRect(i);
            _vert[j] = d / count;
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (!IsExecuteFrame(ref _frameMain)) return;
            
            if (_needUpdateCropping) {
                UpdateCropping(_cropping);
            }
            
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