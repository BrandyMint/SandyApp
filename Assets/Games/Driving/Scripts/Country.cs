using DepthSensorSandbox.Visualisation;
using Games.Common;
using UnityEngine;
using Utilities;

namespace Games.Driving {
    public class Country : MonoBehaviour {
        private static readonly int _ROAD_TEX = Shader.PropertyToID("_RoadTex");
        private static readonly int _ROAD_TEX_PROJ = Shader.PropertyToID("_RoadTex_Proj");
        
        [SerializeField] private Camera _camRoad;
        [SerializeField] private int _roadMapHeight = 512;
        [SerializeField] private Transform _roadTexProjection;
        
        private RenderTexture _roadTex;
        private Vector3 _initSize;

        private void Awake() {
            _initSize = transform.localScale;
            gameObject.SetActive(false);
        }

        private void OnDestroy() {
            if (_roadTex != null)
                Destroy(_roadTex);
        }

        public void ReInit(SandboxMesh mesh, GameField field) {
            transform.position = field.transform.position;
            transform.rotation = field.transform.rotation;
            transform.localScale = _initSize * field.Scale;
            var aspect = field.transform.localScale.x / field.transform.localScale.y;
            _roadTexProjection.localScale = new Vector3(aspect, 1f, 1f);
            _roadTexProjection.localPosition = - new Vector3(aspect, 1f, 0f) / 2f;
            _camRoad.orthographicSize = field.Scale / 2f;
            _camRoad.nearClipPlane = transform.localScale.z / 10f;
            _camRoad.farClipPlane = transform.localScale.z;
            gameObject.SetActive(true);
            if (TexturesHelper.ReCreateIfNeed(ref _roadTex, (int) (_roadMapHeight * aspect), _roadMapHeight)) {
                _roadTex.wrapMode = TextureWrapMode.Clamp;
                _camRoad.targetTexture = _roadTex;
                _camRoad.Render();
            }
            var props = mesh.PropertyBlock;
            props.SetTexture(_ROAD_TEX, _roadTex);
            props.SetMatrix(_ROAD_TEX_PROJ, _roadTexProjection.worldToLocalMatrix);
            mesh.PropertyBlock = props;
        }
    }
}