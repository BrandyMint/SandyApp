using System.Collections;
using UnityEngine;
using Utilities;

namespace Games.Spray {
    public class Spray : MonoBehaviour {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        private static readonly int _DRAWER_POS = Shader.PropertyToID("_DrawerPos");
        private static readonly int _WORLD_TO_DRAWER_MATRIX = Shader.PropertyToID("_WorldToDrawerMatrix");
        private static readonly int _PROJ_MATRIX = Shader.PropertyToID("_ProjMatrix");
        private static readonly int _DRAWER_DEPTH = Shader.PropertyToID("_DrawerDepth");
        
        [SerializeField] private Color _color = Color.red;
        [SerializeField] private ParticleSystem _particle;
        [SerializeField] private Camera _cam;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Collider _collider;
        [SerializeField] private GameObject[] _objsToHide;
        [SerializeField] private int _depthMapResolution;
        [SerializeField] private Material _drawMat;
        [SerializeField] private Material _depthMat;
        
        private bool _fire;
        private RenderTexture _depth;

        public bool Fire {
            get => _fire;
            set {
                if (_fire != value) {
                    _fire = value;
                    ChangeFire(value);
                }
            }
        }

        private void Awake() {
            _renderer.material.color = _color;
            var property = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(property);
            property.SetColor(_COLOR, _color);
            _renderer.SetPropertyBlock(property);
            
            var module = _particle.main;
            module.startColor = _color;
            var shape = _particle.shape;
            shape.angle = _cam.fieldOfView;
            _cam.SetReplacementShader(_depthMat.shader, "RenderType");
        }

        private void OnDestroy() {
            if (_cam != null)
                _cam.targetTexture = null;
            if (_depth != null) {
                _depth.Release();
            }
        }

        private void ChangeFire(bool fire) {
            _collider.enabled = !fire;
            if (fire) {
                _particle.Play();
                StartCoroutine(nameof(RenderingDepth));
            } else {
                StopCoroutine(nameof(RenderingDepth));
                _particle.Stop();
            }
        }

        public void Show(bool show) {
            if (!show) Fire = false;

            foreach (var obj in _objsToHide) {
                obj.SetActive(show);
            }
        }

        public IEnumerator RenderingDepth() {
            yield break;
            while (true) {
                RenderDepth();
                yield return null;
            }
        }

        public void RenderDepth() {
            if (TexturesHelper.ReCreateIfNeed(ref _depth, _depthMapResolution, _depthMapResolution, 0,
                RenderTextureFormat.RFloat)) {
                _cam.targetTexture = _depth;
            }
            _cam.Render();
            
            var projMatrix = _cam.projectionMatrix;
            var worldToDrawerMatrix = transform.worldToLocalMatrix;

            _drawMat.SetVector(_DRAWER_POS, transform.position);
            _drawMat.SetColor(_COLOR, _color);
            _drawMat.SetMatrix(_WORLD_TO_DRAWER_MATRIX, worldToDrawerMatrix);
            _drawMat.SetMatrix(_PROJ_MATRIX, projMatrix);
            //_drawMat.SetTexture("_Cookie", cookie);
            _drawMat.SetTexture(_DRAWER_DEPTH, _depth);
        }
    }
}