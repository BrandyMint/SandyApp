using System;
using System.Threading.Tasks;
using DepthSensor.Buffer;
using DepthSensorSandbox.Processing;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepthSensorSandbox.Visualisation {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SandboxMesh : MonoBehaviour, ISandboxBounds {
        private const string _CALC_DEPTH = "CALC_DEPTH";
        private static readonly int _DEPTH_TEX = Shader.PropertyToID("_DepthTex");
        private static readonly int _MAP_TO_CAMERA_TEX = Shader.PropertyToID("_MapToCameraTex");

        [SerializeField] private bool _updateMeshOnGPU;
        [SerializeField] private bool _needNormalsOnCPU;

        public bool UpdateMeshOnGpu {
            get => _updateMeshOnGPU;
            set => SetUpdateMeshOnGPU(value);
        }

        private MeshFilter _meshFilter;
        private Mesh _mesh;
        protected Vector3[] _vert;
        private Vector3[] _normals;
        protected int[] _triangles;
        protected Vector2[] _uv;
        private Renderer _r;
        private Material _mat;
        private MaterialPropertyBlock _propBlock;
        private bool _prevUpdateMeshOnGPU;
        private bool _needUpdateBounds = true;
        private bool _isBoundsValid;
        protected Sampler _s = Sampler.Create();
        protected Rect _cropping = Sampler.FULL_CROPPING;
        protected bool _needUpdateCropping;
        protected bool _needUpdateUVAndIndexes;

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

            DepthSensorSandboxProcessor.OnCroppingChanged += OnCroppingChanged;
        }

        private void OnCroppingChanged(Rect rect) {
            _cropping = DepthSensorSandboxProcessor.Instance.GetCroppingExtended();
            _needUpdateCropping = true;
        }

        private void Start() {
            if (DepthSensorSandboxProcessor.Instance) {
                OnCroppingChanged(DepthSensorSandboxProcessor.Instance.GetCroppingExtended());
            }

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
            DepthSensorSandboxProcessor.OnCroppingChanged -= OnCroppingChanged;
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
        
        public static bool ReInitMeshIfNeed(Sampler s, ref Vector3[] v, ref int[] tr, bool force = false) {
            var updated = false;
            if ((updated = ReInitVertsIfNeed(s, ref v)) || force) {
                return ReInitTrianglesIfNeed(s, ref tr, force) || updated;
            }
            return false;
        }

        public static bool ReInitMeshIfNeed(Sampler s, ref Vector3[] v, ref int[] tr, ref Vector2[] uv, bool force = false) {
            var updated = false;
            if ((updated = ReInitMeshIfNeed(s, ref v, ref tr, force)) || force) {
                return ReInitUVIfNeed(s, ref uv, force) || updated;
            }
            return false;
        }

        public virtual bool ReInitMeshIfNeed(int width, int height) {
            _s.SetDimens(width, height);
            var needRecalcUVAndIndexes = _needUpdateCropping;
            if (_needUpdateCropping) {
                _s.SetCropping01(_cropping);
                _needUpdateCropping = false;
            }
            var updated = ReInitMeshIfNeed(_s, ref _vert, ref _triangles, ref _uv, needRecalcUVAndIndexes);
            _needUpdateUVAndIndexes |= updated;
            return updated;
        }

        public static bool ReInitVertsIfNeed(Sampler s, ref Vector3[] v) {
            var r = s.Rect;
            var len = r.width * r.height;
            if (v == null || v.Length != len) {
                v = new Vector3[len];
                return true;
            }
            return false;
        }
        
        public static bool ReInitUVIfNeed(Sampler s, ref Vector2[] uv, bool force = false) {
            var r = s.Rect;
            var len = r.width * r.height;
            if (uv == null || uv.Length != len) {
                uv = new Vector2[len];
                force = true;
            }
            if (force) {
                var uvCalc = uv;
                var d = new Vector2(1f / s.width, 1f / s.height) * 0.5f;
                s.EachParallelHorizontal(i => {
                    var j = s.GetIInRect(i);
                    uvCalc[j] = d + new Vector2(
                        (float)(i % s.width) / s.width,
                        (float)(i / s.width) / s.height
                    );
                });
                return true;
            }

            return false;
        }

        public static bool ReInitTrianglesIfNeed(Sampler s, ref int[] tr, bool force = false) {
            var r = s.Rect;
            var quadIndexes = 3 * 2;
            var indexCount = ((uint) r.width - 1) * (r.height - 1) * quadIndexes;

            if (tr == null || tr.LongLength != indexCount) {
                tr = new int[indexCount];
                force = true;
            }

            if (force) {
                var trCalc = tr;
                Parallel.For(0, indexCount / quadIndexes, iQuad => {
                    var iVert = (int) (iQuad + iQuad / (r.width - 1));
                    var i = iQuad * quadIndexes;

                    trCalc[i] = iVert;
                    trCalc[i + 1] = trCalc[i + 3] = iVert + 1;
                    trCalc[i + 2] = trCalc[i + 5] = iVert + r.width;
                    trCalc[i + 4] = iVert + r.width + 1;
                });
                return true;
            }

            return false;
        }

        private void OnDepthDataCPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            ReInitMeshIfNeed(depth.width, depth.height);
            _currDepth = depth;
            _currMapToCamera = mapToCamera;
            _s.EachParallelHorizontal(UpdateMeshBody);
            if (_needNormalsOnCPU) {
                ReInitVertsIfNeed(_s, ref _normals);
                _s.EachParallelHorizontal(UpdateMeshNormalsBody);
            }
        }

        protected DepthBuffer _currDepth;
        protected MapDepthToCameraBuffer _currMapToCamera;

        protected virtual void UpdateMeshBody(int i) {
            var j = _s.GetIInRect(i);
            _vert[j] = PointDepthToVector3(_currDepth, _currMapToCamera, i);
        }
        
        private Vector3[] _normalRhs = {
            Vector3.right,
            Vector3.up, 
            Vector3.left,
            Vector3.down
        };
        private void UpdateMeshNormalsBody(int i) {
            var ir = _s.GetIInRect(i);
            var cross = Vector3.zero;
            var v = _vert[ir];
            for (int k = 0; k < 4; ++k) {
                var j = _s.GetIndexOfNeighbor(i, k);
                if (j != Sampler.INVALID_ID) {
                    var jr = _s.GetIInRect(j);
                    cross += Vector3.Cross(v - _vert[jr], _normalRhs[k]);
                }
            }
            _normals[ir] = cross.normalized;
        }

        public static Vector3 PointDepthToVector3(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera, int i) {
            var xy = mapToCamera.data[i];
            var ud = depth.data[i];
            var d = ud != 0 ? (float) ud / 1000f : float.NaN;
            return new Vector3(xy.x * d, xy.y * d, d);
        }

        private void OnNewFrameCPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (_vert != null && _triangles != null) {
                if (_needUpdateUVAndIndexes)
                    _mesh.Clear();
                if (!_updateMeshOnGPU || _mesh.vertexCount != _vert.Length || _needUpdateBounds || _needUpdateUVAndIndexes) {
                    _mesh.vertices = _vert;
                    if (_needNormalsOnCPU)
                        _mesh.normals = _normals;
                }
                if (_needUpdateUVAndIndexes) {
                    _needUpdateUVAndIndexes = false;
                    _mesh.uv = _uv;
                    _mesh.indexFormat = _vert.Length > UInt16.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    _mesh.triangles = _triangles;
                    _needUpdateBounds = true;
                }

                if (_needUpdateBounds) {
                    _needUpdateBounds = false;
                    _mesh.RecalculateBounds();
                    _isBoundsValid = true;
                }
            }
        }

        private void OnDepthDataGPU(DepthBuffer depth, MapDepthToCameraBuffer mapToCamera) {
            if (ReInitMeshIfNeed(depth.width, depth.height) || _needUpdateBounds)
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