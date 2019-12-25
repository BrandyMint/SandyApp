using System;
using System.Threading.Tasks;
using DepthSensor.Buffer;
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
            DepthSensorSandboxProcessor.OnDepthDataBackground += OnDepthData;
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthData;
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }

        private void OnDepthData(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (!IsExecuteFrame(ref _frameBG)) return;
            
            var scale = Mathf.RoundToInt((float) depth.height / Mathf.Min(depth.height, _maxHeight));
            int height = depth.height / scale;
            int width = depth.width / scale;
            SandboxMesh.ReInitMeshIfNeed(width, height, ref _vert, ref _triangles);
            var scale2 = Mathf.RoundToInt((float) scale / 2);
            Parallel.For(0, height, y => {
                var i = y * width;
                var ii = scale * y * depth.width;
                for (var x = 0; x < width; ++x) {
                    var d = Vector3.zero;
                    var count = 0; 
                    for (var xd = -scale2; xd < scale2; ++xd) {
                        var xx = scale * x + xd;
                        if (xx >= 0 && xx < depth.width) {
                            d += SandboxMesh.PointDepthToVector3(depth, mapToCamera, ii + xd);
                            ++count;
                        }
                    }
                    _vert[i] = d / count;
                    ++i;
                    ii += scale;
                }
            });
        }

        private void OnNewFrame(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (!IsExecuteFrame(ref _frameMain)) return;
            
            if (_vert != null && _triangles != null) {
                _mesh.vertices = _vert;
                if (_mesh.GetIndexCount(0) != _triangles.LongLength) {
                    _mesh.indexFormat = _vert.Length > UInt16.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    _mesh.triangles = _triangles;
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