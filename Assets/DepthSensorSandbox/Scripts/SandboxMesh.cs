using System.Threading.Tasks;
using DepthSensor.Stream;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SandboxMesh : MonoBehaviour {
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
        private bool _prevUpdateMeshOnGPU;
        private bool _firstFrame = true;
        private bool _isBoundsValid;

        private void Awake() {
            _r = GetComponent<MeshRenderer>();
            _mat = _r.material;
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh {name = "depth"};
            _mesh.MarkDynamic();
            _mesh.OptimizeReorderVertexBuffer();
            _meshFilter.mesh = _mesh;
            _mesh.indexFormat = IndexFormat.UInt32;
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
            set { _r.material = _mat = value; InitMaterial(); }
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
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrameGPU;
                DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthDataCPU;
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrameCPU;
            } else {
                DepthSensorSandboxProcessor.OnDepthDataBackground += OnDepthDataCPU;
                DepthSensorSandboxProcessor.OnNewFrame += OnNewFrameCPU;
                DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthDataGPU;
                DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrameGPU;
            }
            _updateMeshOnGPU = _prevUpdateMeshOnGPU = onGPU;
            InitMaterial();
        }

        private void InitMaterial() {
            if (_updateMeshOnGPU) {
                _mat.EnableKeyword(_CALC_DEPTH);
            } else {
                _mat.DisableKeyword(_CALC_DEPTH);
            }
        }

        private void ReInitMeshIfNeed(int width, int heigth, int len) {
            if (_vert == null || _vert.Length != len) {
                _vert = new Vector3[len];
                _uv = new Vector2[len];
                var d = new Vector2(1f / width, 1f / heigth) * 0.5f;
                Parallel.For(0, len, i => {
                    _uv[i] = d + new Vector2(
                        (float)(i % width) / width,
                        (float)(i / width) / heigth
                    );
                });
            }

            var quadIndexes = 3 * 2;
            var indexCount =  ((uint)width - 1) * (heigth - 1) * quadIndexes;
            
            if (_triangles == null || _triangles.LongLength != indexCount) {
                _triangles = new int[indexCount];
                
                Parallel.For(0, indexCount / quadIndexes, iQuad => {
                    var iVert = (int) (iQuad + iQuad / (width - 1));
                    var i = iQuad * quadIndexes;
                    
                    _triangles[i] = iVert;
                    _triangles[i + 1] = _triangles[i + 3] = iVert + 1;
                    _triangles[i + 2] = _triangles[i + 5] = iVert + width;
                    _triangles[i + 4] = iVert + width + 1;
                });
            }
        }

        private void OnDepthDataCPU(DepthStream depth, MapDepthToCameraStream mapToCamera) {
            ReInitMeshIfNeed(depth.width, depth.height, depth.data.Length);
            Parallel.For(0, depth.data.Length, i => {
                var xy = mapToCamera.data[i];
                var ud = depth.data[i];
                var d = ud != 0 ? (float) ud / 1000f : float.NaN;
                _vert[i] = new Vector3(xy.x * d, xy.y * d, d);
            });
        }

        private void OnNewFrameCPU(DepthStream depth, MapDepthToCameraStream mapToCamera) {
            if (_vert != null && _triangles != null) {
                if (!_updateMeshOnGPU || _mesh.vertexCount != _vert.Length || _firstFrame)
                    _mesh.vertices = _vert;
                if (_mesh.GetIndexCount(0) != _triangles.LongLength) {
                    _mesh.uv = _uv;
                    _mesh.triangles = _triangles;
                }

                if (_firstFrame) {
                    _firstFrame = false;
                    _mesh.RecalculateBounds();
                    _isBoundsValid = true;
                }
            }
        }

        private void OnDepthDataGPU(DepthStream depth, MapDepthToCameraStream mapToCamera) {
            ReInitMeshIfNeed(depth.width, depth.height, depth.data.Length);
            if (_firstFrame)
                OnDepthDataCPU(depth, mapToCamera);
        }

        private void OnNewFrameGPU(DepthStream depth, MapDepthToCameraStream mapToCamera) {
            _mat.SetTexture(_DEPTH_TEX, depth.texture);
            _mat.SetTexture(_MAP_TO_CAMERA_TEX, mapToCamera.texture);

            OnNewFrameCPU(depth, mapToCamera);
        }
    }
}