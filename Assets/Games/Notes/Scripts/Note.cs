using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Notes {
    public class Note : InteractableModel {

        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _tCurr;
        private float _tFull;
        private float _tAccept;
        private bool _isGoing = false;

        public void Go(Bounds b, Vector3 v, float t, float sizeAccept) {
            _startPos = EndPosInDir(b, -v);
            _endPos = EndPosInDir(b, v);
            _tFull = _tCurr = t;
            _tAccept = sizeAccept / 2f / Vector3.Distance(_startPos, _endPos) * t;
            transform.position = _startPos;
            _isGoing = true;
        }

        private static Vector3 EndPosInDir(Bounds b, Vector3 dir) {
            return b.center + dir.normalized * Vector3.Project(b.extents, dir).magnitude;
        }

        public bool IsGoodTimeToBang => _tCurr < _tAccept && !IsLose;
        public bool IsLose => _tCurr < -_tAccept * 2f;

        public bool IsGoing => _isGoing;

        private void Update() {
            if (!_isGoing) return;
            _tCurr -= Time.deltaTime;
            transform.position = Vector3.LerpUnclamped(_endPos, _startPos, _tCurr / _tFull);
            if (IsLose)
                Bang(false);
        }

        public override void Bang(bool isRight) {
            _isGoing = false;
            base.Bang(isRight);
        }
    }
}