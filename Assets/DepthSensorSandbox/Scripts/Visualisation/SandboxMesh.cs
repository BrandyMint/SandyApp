using System;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SandboxMesh : MonoBehaviour, ISandboxBounds {
        private const string _CALC_DEPTH = "CALC_DEPTH";
        private static readonly int _DEPTH_TEX = Shader.PropertyToID("_DepthTex");
        private static readonly int _MAP_TO_CAMERA_TEX = Shader.PropertyToID("_MapToCameraTex");

        [SerializeField] private bool _updateMeshOnGPU;

        public bool UpdateMeshOnGpu {
            get => _updateMeshOnGPU;
            set => SetUpdateMeshOnGPU(value);
        }

        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Vector3[] _vert;
        private int[] _triangles;
        private Vector2[] _uv;
        private Renderer _r;
        private Material _mat;
        private MaterialPropertyBlock _propBlock;
        private bool _prevUpdateMeshOnGPU;
        private bool _needUpdateBounds = true;
        private bool _isBoundsValid;

        private void Awake() {
            _r = GetComponent<MeshRenderer>();
            _mat = _r.material;
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = _meshFilter.mesh;
            _propBlock = new MaterialPropertyBlock();
            if (_mesh == null || _mesh.vertexCount == 0) {
                _mesh = new Mesh {name = "depth"};
                _mesh.MarkDynamic();
                _meshFilter.mesh = _mesh;
            }
        }

        private void Start() {
            SetUpdateMeshOnGPU(_updateMeshOnGPU, true);
        }

        public Bounds GetBounds() {
            return _mesh.bounds;
        }

        public bool IsBoundsValid() {
            return _isBoundsValid;
        }

        public Material Material {
            get => _mat;
            set { _r.material = _mat = value; }
        }

        public MaterialPropertyBlock PropertyBlock {
            get {
                _r.GetPropertyBlock(_propBlock);
                return _propBlock;
            }
            set => _r.SetPropertyBlock(value);
        }

        public void AddDrawToCommandBuffer(CommandBuffer cmb, Material m = null) {
            if (m == null) m = _mat;
            cmb.DrawRenderer(_r, m);
        }

        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthDataGPU;
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrameGPU;
            DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthDataCPU;
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrameCPU;
        }
        
#if UNITY_EDITOR
        private void FixedUpdate() {
            UpdateMeshOnGpu = _updateMeshOnGPU;
        }
#endif

        private void SetUpdateMeshOnGPU(bool onGPU, bool force = false) {
            if (_prevUpdateMeshOnGPU == onGPU && !force)
                return;
            if (onGPU) {
                DepthSensorSandboxProcessor.OnDepthDataBackground += OnDepthDataGPU;
                DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthDataCPU;
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrameGPU;
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrameCPU;
                Shader.EnableKeyword(_CALC_DEPTH);
            } else {
                Shader.DisableKeyword(_CALC_DEPTH);
                DepthSensorSandboxProcessor.OnDepthDataBackground += OnDepthDataCPU;
                DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthDataGPU;
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrameCPU;
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrameGPU;
            }
            _updateMeshOnGPU = _prevUpdateMeshOnGPU = onGPU;
        }

        public static bool ReInitMeshIfNeed(int width, int height, ref Vector3[] v, ref int[] tr) {
            if (ReInitVertsIfNeed(width, height, ref v)) {
                ReInitTrianglesIfNeed(width, height, ref tr);
                return true;
            }
            return false;
        }

        public static bool ReInitMeshIfNeed(int width, int height, ref Vector3[] v, ref int[] tr, ref Vector2[] uv) {
            if (ReInitMeshIfNeed(width, height, ref v, ref tr)) {
                ReInitUVIfNeed(width, height, ref uv);
                return true;
            }
            return false;
        }

        public static bool ReInitVertsIfNeed(int width, int height, ref Vector3[] v) {
            var len = width * height;
            if (v == null || v.Length != len) {
                v = new Vector3[len];
                return true;
            }
            return false;
        }
        
        public static bool ReInitUVIfNeed(int width, int height, ref Vector2[] uv) {
            var len = width * height;
            if (uv == null || uv.Length != len) {
                var uvCalc = uv = new Vector2[len];
                var d = new Vector2(1f / width, 1f / height) * 0.5f;
                Parallel.For(0, len, i => {
                    uvCalc[i] = d + new Vector2(
                        (float)(i % width) / width,
                        (float)(i / width) / height
                    );
                });
                return true;
            }

            return false;
        }
        
        public static bool ReInitTrianglesIfNeed(int width, int height, ref int[] tr) {
            var quadIndexes = 3 * 2;
            var indexCount =  ((uint)width - 1) * (height - 1) * quadIndexes;
            
            if (tr == null || tr.LongLength != indexCount) {
                var trCalc = tr = new int[indexCount];
                
                Parallel.For(0, indexCount / quadIndexes, iQuad => {
                    var iVert = (int) (iQuad + iQuad / (width - 1));
                    var i = iQuad * quadIndexes;
                    
                    trCalc[i] = iVert;
                    trCalc[i + 1] = trCalc[i + 3] = iVert + 1;
                    trCalc[i + 2] = trCalc[i + 5] = iVert + width;
                    trCalc[i + 4] = iVert + width + 1;
                });
                return true;
            }

            return false;
        }

        private void OnDepthDataCPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            ReInitMeshIfNeed(depth.width, depth.height, ref _vert, ref _triangles, ref _uv);
            Parallel.For(0, depth.length, i => {
                _vert[i] = PointDepthToVector3(depth, mapToCamera, i);
            });
        }

        public static Vector3 PointDepthToVector3(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera, int i) {
            var xy = mapToCamera.data[i];
            var ud = depth.data[i];
            var d = ud != 0 ? (float) ud / 1000f : float.NaN;
            return new Vector3(xy.x * d, xy.y * d, d);
        }

        private void OnNewFrameCPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (_vert != null && _triangles != null) {
                if (!_updateMeshOnGPU || _mesh.vertexCount != _vert.Length || _needUpdateBounds)
                    _mesh.vertices = _vert;
                if (_mesh.GetIndexCount(0) != _triangles.LongLength) {
                    _mesh.uv = _uv;
                    _mesh.indexFormat = _vert.Length > UInt16.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    _mesh.triangles = _triangles;
                }

                if (_needUpdateBounds) {
                    _needUpdateBounds = false;
                    _mesh.RecalculateBounds();
                    _isBoundsValid = true;
                }
            }
        }

        private void OnDepthDataGPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            ReInitMeshIfNeed(depth.width, depth.height, ref _vert, ref _triangles, ref _uv);
            if (_needUpdateBounds)
                OnDepthDataCPU(depth, mapToCamera);
        }

        private void OnNewFrameGPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            var props = PropertyBlock;
            props.SetTexture(_DEPTH_TEX, depth.texture);
            props.SetTexture(_MAP_TO_CAMERA_TEX, mapToCamera.texture);
            PropertyBlock = props;

            OnNewFrameCPU(depth, mapToCamera);
        }

        public void RequestUpdateBounds() {
            _isBoundsValid = false;
            _needUpdateBounds = true;
        }
    }
}