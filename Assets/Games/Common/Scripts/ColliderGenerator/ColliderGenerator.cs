using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public class ColliderGenerator {
        private class Vector2IntComparer : IComparer<Vector2Int> {
            public int Compare(Vector2Int p1, Vector2Int p2) {
                if (p1.y == p2.y) {
                    return p1.x - p2.x;
                } else {
                    return p1.y - p2.y;
                } 
            }
        }

        private static readonly Vector2Int[] _steps = {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
        };

        private IColliderGeneratorDataProvider _data;
        private IColliderGeneratorOutput _collider;
        private RectInt _rect;
        private bool[,] _was;

        private readonly SortedSet<Vector2Int> _scan = new SortedSet<Vector2Int>(new Vector2IntComparer());

        public void Generate(IColliderGeneratorDataProvider data, IColliderGeneratorOutput collider) {
            _data = data;
            _collider = collider;
            _rect = data.Rect;
            ReCreateIfNeed(ref _was, _rect.width, _rect.height);
            Clear(_was, false);
            _scan.Clear();
            Scan();
            while (_scan.Any()) {
                FindShape(_scan.First());
            }
        }
        
        private void Scan() {
            var p = Vector2Int.zero;
            for (p.y = _rect.yMin; p.y < _rect.yMax; ++p.y) {
                var wasShape = false;
                for (p.x = _rect.xMin; p.x < _rect.xMax; ++p.x) {
                    var isShape = _data.IsShapePixel(p);
                    if (!wasShape && isShape)
                        _scan.Add(p);
                    wasShape = isShape;
                }
            }
        }

        private bool FindShape(Vector2Int p) {
            _scan.Remove(p);
            var shape = new List<Vector2> {p};
            var dir = 0;
            while (true) {
                if (FindGoDir(dir, p, ref p, ref dir, out var needNewShape)) {
                    if (AddPointAndSendShapeIfNeed(p, shape, false)) return true;
                } else {
                    return false;
                }

                FindNextPointInDir(dir, p, ref p);
                if (AddPointAndSendShapeIfNeed(p, shape)) return true;
            }

            return false;
        }

        public bool AddPointAndSendShapeIfNeed(Vector2Int p, List<Vector2> shape, bool put = true, bool forceSend = false) {
            if (GetWas(p) || forceSend) {
                _collider.AddShape(shape);
                return true;
            }

            if (put) {
                shape.Add(p);
                SetWas(p);
            }
            return false;
        }
        
        public bool FindGoDir(int startDir, Vector2Int p, ref Vector2Int outNextP, ref int outNextDir, out bool needNewShape) {
            if (GoShapeEdge(startDir, p, ref outNextP)) {
                outNextDir = startDir;
                needNewShape = false;
                return true;
            }
            for (int inc = 1; inc <= _steps.Length / 2; ++inc) {
                var currDir = ClockwiseDir(startDir, inc);
                if (GoShapeEdge(currDir, p, ref outNextP)) {
                    outNextDir = currDir;
                    needNewShape = false;
                    return true;
                }
                currDir = ClockwiseDir(startDir, -inc);
                if (GoShapeEdge(currDir, p, ref outNextP)) {
                    outNextDir = currDir;
                    needNewShape = true;
                    return true;
                }
            }

            needNewShape = false;
            return false;
        }

        public bool FindNextPointInDir(int dir, Vector2Int p, ref Vector2Int nextP) {
            bool wasStep = false;
            while (GoShapeEdge(dir, p, ref p)) {
                wasStep = true;
            }
            if (wasStep) {
                nextP = p;
            }
            return wasStep;
        }

        public int ClockwiseDir(int dir, int inc) {
            return (_steps.Length + dir + inc) % _steps.Length;
        }

        public bool GoShapeEdge(int dir, Vector2Int p, ref Vector2Int outNextP) {
            var nextPos = p + _steps[dir];
            if (_rect.Contains(nextPos) && _data.IsShapePixel(nextPos)) {
                var leftPos = p + _steps[ClockwiseDir(dir, -1)];
                if (!_rect.Contains(leftPos) || !_data.IsShapePixel(leftPos)) {
                    outNextP = nextPos;
                    _scan.Remove(p);
                    return true;
                }
            }

            return false;
        }
        
        private void SetWas(Vector2Int p, bool was = true) {
            _was[p.x - _rect.xMin, p.y - _rect.yMin] = was;
        }
        
        private bool GetWas(Vector2Int p) {
            return _was[p.x - _rect.xMin, p.y - _rect.yMin];
        }
        
        protected static bool ReCreateIfNeed<T>(ref T[,] a, int w, int h) {
            if (a == null || a.GetLength(0) != w || a.GetLength(1) != h) {
                a = new T[w, h];
                return true;
            }
            return false;
        }
        
        private static void Clear<T>(T[,] a, T val) {
            for (int i = 0; i < a.GetLength(0); ++i) {
                for (int j = 0; j < a.GetLength(1); ++j) {
                    a[i, j] = val;
                }
            }
        }
    }
}