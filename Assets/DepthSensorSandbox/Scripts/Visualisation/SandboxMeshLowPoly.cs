using DepthSensorSandbox.Processing;
using UnityEngine;

namespace DepthSensorSandbox.Visualisation {
    public class SandboxMeshLowPoly : SandboxMesh {
        [SerializeField] private int _meshHeight = 32;
        
        protected Sampler _sFull = Sampler.Create();
        
        public override bool ReInitMeshIfNeed(int width, int height) {
            _sFull.SetDimens(width, height);
            var needRecalcUVAndIndexes = _needUpdateCropping;
            if (_needUpdateCropping) {
                _sFull.SetCropping01(_cropping);
                _s.SetCropping01(_cropping);
                _needUpdateCropping = false;
            }
            var rFull = _sFull.Rect;
            var downsize = rFull.height / _meshHeight;
            _s.SetDimens(width / downsize, height / downsize);
            var updated = ReInitMeshIfNeed(_s, ref _vert, ref _triangles, ref _uv, needRecalcUVAndIndexes);
            _needUpdateUVAndIndexes |= updated;
            return updated;
        }

        protected override void UpdateMeshBody(int i) {
            var j = _s.GetIInRect(i);
            i = _sFull.GetIConverted(_s, i);
            _vert[j] = PointDepthToVector3(_currDepth, _currMapToCamera, i);
        }
    }
}