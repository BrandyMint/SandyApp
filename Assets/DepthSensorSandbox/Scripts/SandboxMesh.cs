using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepthSensorSandbox {
    [RequireComponent(typeof(MeshFilter))]
    public class SandboxMesh : MonoBehaviour {
        private const float _MAX_DEPTH = 10f;
        
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Vector3[] _vert;
        private int[] _triangles;
        private Vector2[] _uv;

        private void Awake() {
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh {name = "depth"};
            _mesh.MarkDynamic();
            _meshFilter.mesh = _mesh;
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * _MAX_DEPTH);
        }

        private void Start() {
            DepthSensorSandboxProcessor.OnDepthDataBackground += OnDepthData;
            DepthSensorSandboxProcessor.OnNewFrame += OnNewFrame;
        }
        
        private void OnDestroy() {
            DepthSensorSandboxProcessor.OnDepthDataBackground -= OnDepthData;
            DepthSensorSandboxProcessor.OnNewFrame -= OnNewFrame;
        }

        private void ReInitMesh(int width, int heigth, int len) {
            if (_vert == null || _vert.Length != len) {
                _vert = new Vector3[len];
                _uv = new Vector2[len];
                Parallel.For(0, len, i => {
                    _uv[i] =  new Vector2(
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

        private void OnDepthData(int width, int height, ushort[] depth, Vector2[] mapToCamera) {
            ReInitMesh(width, height, depth.Length);
            Parallel.For(0, depth.Length, i => {
                var xy = mapToCamera[i];
                var ud = depth[i];
                var d = ud != 0 ? (float) ud / 1000f : float.NaN;
                _vert[i] = new Vector3(xy.x * d, xy.y * d, d);
            });
        }

        private void OnNewFrame(int width, int height, ushort[] depth, Vector2[] mapToCamera) {
            if (_vert != null && _triangles != null) {
                _mesh.vertices = _vert;
                if (_mesh.GetIndexCount(0) != _triangles.LongLength) {
                    _mesh.uv = _uv;
                    _mesh.triangles = _triangles;
                }
            }
        }
    }
}