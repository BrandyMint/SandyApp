using System.Collections.Generic;
using BezierSolution;
using UnityEngine;

namespace Games.Road {
    [ExecuteInEditMode]
    public class LineRendererBezier : MonoBehaviour {
        private static readonly int _MAIN_TEX_ST = Shader.PropertyToID("_MainTex_ST");
        private const float _MIN_STEP = 0.001f;
        
        public float step = 0.1f;
        public float xshift = 0f;
        public Vector3 up = Vector3.up;
        public BezierSpline spline;
        
        private LineRenderer _line;
        private readonly List<Vector3> _points = new List<Vector3>();
        private MaterialPropertyBlock _props;

        public float Width {
            get => _line.startWidth;
            set {
                _line.startWidth = value;
                _line.endWidth = value;
            }
            
        }

        private void Awake() {
            _line = GetComponent<LineRenderer>();
            _props = new MaterialPropertyBlock();
        }

#if UNITY_EDITOR
        private void OnEnable() {
            Awake();
        }
#endif

#if UNITY_EDITOR
        private void LateUpdate() {
            if (!UnityEditor.EditorApplication.isPlaying)
                UpdateLine();
        }
#endif

        /*private void FixedUpdate() {
            UpdateLine();
        }*/

        public void UpdateLine() {
            if (spline == null || _line == null || step < _MIN_STEP)
                return;

            _points.Clear();

            var t = 0f;
            while (t < 1f) { 
                AddPoint(t);
                spline.MoveAlongSpline(ref t, step);
            }

            if (!spline.loop) {
                AddPoint(1f);
            }

            _line.positionCount = _points.Count;
            _line.SetPositions(_points.ToArray());
            _line.loop = spline.loop;
            
            _line.GetPropertyBlock(_props);
            var m = _line.sharedMaterial;
            var st = m == null ? new Vector4(1f, 1f, 0f, 0f) : m.GetVector(_MAIN_TEX_ST);
            st.x *= spline.Length / Width;
            _props.SetVector(_MAIN_TEX_ST, st);
            _line.SetPropertyBlock(_props);
        }

        private void AddPoint(float t) {
            var p = spline.GetPoint(t);
            var right = Vector3.Cross(spline.GetTangent(t), up).normalized;
            p += right * xshift;
            _points.Add(p);
        }
    }
}