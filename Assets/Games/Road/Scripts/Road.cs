using BezierSolution;
using UnityEngine;

namespace Games.Road {
    [ExecuteInEditMode]
    public class Road : MonoBehaviour {
        [SerializeField] private BezierSpline _spline;
        [SerializeField] private LineRendererBezier[] _lines;
        public float width = 2f;
        
        private void Awake() {
        }

#if UNITY_EDITOR
        private void OnEnable() {
            Awake();
        }
#endif

#if UNITY_EDITOR
        private void LateUpdate() {
            if (IsStoppedEditor())
                SetPath(_spline);
        }
#endif
        private bool IsStoppedEditor() {
#if UNITY_EDITOR
            return !UnityEditor.EditorApplication.isPlaying;
#endif
            return false;
        }
        
        public void UpdateLines() {
            var widthOne = width / _lines.Length;
            var xshift = - widthOne * (_lines.Length % 2 == 0
                             ? (float)(_lines.Length - 1) / 2
                             : _lines.Length / 2
            );
            foreach (var line in _lines) {
                line.Width = widthOne * 1.05f;
                line.xshift = xshift;
                xshift += widthOne;
                if (!IsStoppedEditor())
                    line.UpdateLine();
            }
        }

        public void SetPath(BezierSpline spline) {
            foreach (var line in _lines) {
                line.spline = spline;
            }
            UpdateLines();
        }
    }
}