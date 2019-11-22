using DepthSensorCalibration;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace Games.Spray {
    [RequireComponent(typeof(Renderer))]
    public class ProjectorDestination : MonoBehaviour {
        private static readonly int _PROJECTED_TEX = Shader.PropertyToID("_ProjectedTex");
        private static readonly int _MAIN_TEX = Shader.PropertyToID("_MainTex");
        
        [SerializeField] private Camera _srcCam;
        [SerializeField] private Material _projectedMat;
        [SerializeField] private Material _blendMat;

        private Camera _cam;
        private Renderer _r;
        private Renderer _rProject;
        private MaterialPropertyBlock _props;
        private CameraRenderToTexture _cameraRender;
        private RenderTexture _newFrame;
        private int _projectLayer;
        private bool _needClear;

        private void Awake() {
            _projectedMat = new Material(_projectedMat);
            
            _projectLayer = LayerMask.NameToLayer("projector");
            _props = new MaterialPropertyBlock();
            _r = GetComponent<Renderer>();
            
            _cam = new GameObject("ProjectingCamera").AddComponent<Camera>();
            _cam.transform.SetParent(_srcCam.transform, false);
            _cam.CopyFrom(_srcCam);
            /*_cam.gameObject.AddComponent<CameraFlipper>();
            _cam.gameObject.AddComponent<SandboxCamera>();*/
            _cam.clearFlags = CameraClearFlags.Depth;
            _cam.cullingMask = 1 << _projectLayer;
            _cam.enabled = false;
        }

        private void Start() {
            var proj = new GameObject("ProjectedRenderer") {layer = _projectLayer};
            proj.transform.SetParent(transform, false);
            proj.AddComponent(GetComponent<MeshFilter>());
            _rProject = (Renderer) proj.AddComponent(_r);
            _rProject.material = _projectedMat;
            
            _cameraRender = _cam.gameObject.AddComponent<CameraRenderToTexture>();
            _cameraRender.ManualRender = true;
            _cameraRender.Enable(_blendMat, RenderTextureFormat.ARGBFloat, CameraEvent.AfterForwardAlpha, OnFrame, CreateCommandBuffer);
            
            Clear();
        }

        private void OnDestroy() {
            if (_cameraRender != null) {
                _cameraRender.Disable();
            }
            if (_newFrame != null) {
                if (_cam != null)
                    _cam.targetTexture = null;
                _newFrame.Release();
            }
        }

        private void CreateCommandBuffer(CommandBuffer cmb, Material mat, RenderTexture rt, RenderTargetIdentifier src) {
            if (TexturesHelper.ReCreateIfNeedCompatible(ref _newFrame, rt)) {
                _cam.depth = 16;
                _cam.targetTexture = _newFrame;
            }
            mat.SetTexture(_MAIN_TEX, _newFrame);
            cmb.Blit(src, rt, mat);
        }

        private void FixedUpdate() {
            _r.GetPropertyBlock(_props);
            _rProject.SetPropertyBlock(_props);
            OnFrame(_cameraRender.Render());
        }

        private void OnFrame(RenderTexture frame) {
            if (_needClear) {
                TexturesHelper.Clear(frame);
                _needClear = false;
            }
            
            _r.GetPropertyBlock(_props);
            _props.SetTexture(_PROJECTED_TEX, frame);
            _r.SetPropertyBlock(_props);
        }

        public void Clear() {
            _needClear = true;
        }
    }
}