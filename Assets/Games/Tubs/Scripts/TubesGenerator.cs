using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities;

namespace Games.Tubs {
    public class TubesGenerator : MonoBehaviour {
        private const int _ITERATIONS = 20;
        
        [SerializeField] private Vector2 _borders = Vector2.one * 1.5f;
        [SerializeField] private int _cellsHeight = 7;
        [SerializeField] private float _valveChance = 0.3f;
        [SerializeField] private float _4DirsChance = 0.2f;
        [SerializeField] private float _3DirsChance = 0.2f;
        [SerializeField] private float _3or4MinDist = 2;

        private class TubeCell {
            public TubeFragment tube = null;
            public bool needTube = false;
        }

        private class PathCalcCell {
            public TubeDir dirCome = 0;
            public int cost = 0;
        }
        
        private TubeFragment[] _tubeTpls;
        private TubeCell[,] _tubesField;
        private PathCalcCell[,] _pathField;
        private int _cellsWidth;
        private float _tubeScale;
        private Vector3 _startPos;
        private readonly List<TubeCell> _tubes = new List<TubeCell>();

        private void Awake() {
            _tubeTpls = GetComponentsInChildren<TubeFragment>();
            foreach (var tube in _tubeTpls) {
                tube.Init();
                tube.gameObject.SetActive(false);
            }
        }

        private void Clear() {
            foreach (var cell in _tubes) {
                Destroy(cell.tube.gameObject);
            }
            _tubes.Clear();
            _tubesField = new TubeCell[_cellsWidth, _cellsHeight];
            Clear(_tubesField);
            _pathField = new PathCalcCell[_cellsWidth, _cellsHeight];
        }

        private void Clear<T>(T[,] arr) where T : new() {
            for (int x = 0; x < _cellsWidth; ++x) {
                for (int y = 0; y < _cellsHeight; ++y) {
                    arr[x, y] = new T();
                }
            }
        }

        public IEnumerable<TubeFragment> Tubes => _tubes.Select(c => c.tube);
        
        public void ReGenerate(Transform field) {
            CalcSizes(field, out _tubeScale, out _cellsWidth, out _startPos);
            Clear();
            var createdPoses = new List<Vector2Int>();
            RandomCreate(4, _4DirsChance, createdPoses);
            RandomCreate(3, _3DirsChance, createdPoses);
            foreach (var pose in createdPoses) {
                CreateTubesFrom(pose);
            }
            FixHoles();
        }

        public float TubeSale => _tubeScale;

        private void CalcSizes(Transform field, out float tubeScale, out int cellsWidth, out Vector3 startPos) {
            var min = field.TransformPointTo(transform, new Vector3(-0.5f, -0.5f, 0f));
            var max = field.TransformPointTo(transform, new Vector3(0.5f, 0.5f, 0f));
            tubeScale = (max.y - min.y) / (_cellsHeight + 2f * _borders.y);
            cellsWidth = Mathf.FloorToInt((max.x - min.x - 2f * _borders.x * tubeScale) / tubeScale);
            startPos = min + (max - min - tubeScale * new Vector3(cellsWidth, _cellsHeight) + new Vector3(tubeScale, tubeScale)) / 2f;
        }

        private void RandomCreate(int dirsCount, float chance, List<Vector2Int> createdPoses) {
            var count = (int)(_tubesField.Length * chance);
            for (int c = 0; c < count; ++c) {
                for (int i = 0; i < _ITERATIONS; ++i) {
                    var p = new Vector2Int(Random.Range(0, _cellsWidth), Random.Range(0, _cellsHeight));
                    var cell = GetCell(p);
                    if (cell.needTube || CloseToOthers(createdPoses, p)) continue;

                    var currDirsCount = GetAllowedDirs(p, out var dirs);
                    currDirsCount = RemoveRandomDirs(currDirsCount, dirsCount - currDirsCount, ref dirs);
                    if (currDirsCount != dirsCount) continue;

                    foreach (var d in Tube.DIRS) {
                        if (dirs.HasFlag(d)) {
                            var neighbor = GetCell(Tube.GetPos(p, d));
                            neighbor.needTube = neighbor.tube == null;
                        }
                    }

                    CreateTube(cell, p, dirs);
                    createdPoses.Add(p);
                    break;
                }
            }
        }

        private void CreateTubesFrom(Vector2Int pose) {
            Clear(_pathField);
            var need = new List<Vector2Int>();
            foreach (var d in Tube.Each(GetCell(pose).tube.Dirs)) {
                var p = Tube.GetPos(pose, d);
                need.Add(p);
                var path = GetPath(p);
                path.dirCome = d;
                path.cost = 1;
            }
            var found = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>(need);
            
            while (queue.Any() && found.Count < need.Count) {
                var current = queue.Dequeue();
                var currPath = GetPath(current);
                foreach (var d in Tube.DIRS) {
                    var next = Tube.GetPos(current, d);
                    if (isValidCell(next)) {
                        var nextCell = GetCell(next);
                        if (nextCell.tube != null) continue;
                        var nextPath = GetPath(next);
                        var cost = currPath.cost + (currPath.dirCome == d ? 1 : 2);
                        if (nextPath.cost != 0 && nextPath.cost <= cost) continue;
                        nextPath.cost = cost;
                        nextPath.dirCome = d;
                        if (nextCell.needTube)
                            found.Add(next);
                        else
                            queue.Enqueue(next);
                    }
                }
            }

            while (found.Any()) {
                var p = found.Random();
                found.Remove(p);
                var cell = GetCell(p);
                if (cell.tube != null) continue;
                TubeDir dir = 0;
                do {
                    var minCost = int.MaxValue;
                    var pMextMin = Vector2Int.zero;
                    TubeCell cellNextMin = null;
                    dir = 0;
                    foreach (var dirNext in Tube.DIRS) {
                        var pNext = Tube.GetPos(p, dirNext);
                        if (!isValidCell(pNext)) continue;
                        var cellNext = GetCell(pNext);
                        if (cellNext.tube != null) continue;
                        var pathNext = GetPath(pNext);
                        if (pathNext.cost != 0 && pathNext.cost < minCost) {
                            minCost = pathNext.cost;
                            pMextMin = pNext;
                            cellNextMin = cellNext;
                            dir = dirNext;
                        }
                    }
                    dir |= GetDirsToNeighbors(p);
                    if (dir != 0) {
                        CreateTube(cell, p, dir);
                    }

                    p = pMextMin;
                    cell = cellNextMin;
                } while (cell != null);
            }
        }

        private void FixHoles() {
            for (int x = 0; x < _cellsWidth; ++x) {
                for (int y = 0; y < _cellsHeight; ++y) {
                    var p = new Vector2Int(x, y);
                    var cell = GetCell(p);
                    if (cell.needTube && cell.tube == null) {
                        var dirs = GetDirsToNeighbors(p);
                        CreateTube(cell, p, dirs);
                    }
                }
            }
        }

        private bool CloseToOthers(IEnumerable<Vector2Int> createdPoses, Vector2Int p) {
            return createdPoses.Any(cp => 
                Vector2.Distance(cp, p) < _3or4MinDist
            );
        }

        private int GetAllowedDirs(Vector2Int p, out TubeDir dirs) {
            dirs = 0;
            var dirsCount = 0;
            foreach (var d in Tube.DIRS) {
                if (isValidCell(Tube.GetPos(p, d))) {
                    dirs |= d;
                    ++dirsCount;
                }
            }
            return dirsCount;
        }

        private int RemoveRandomDirs(int dirsCount, int removeCount, ref TubeDir dirs) {
            var count = Mathf.Min(dirsCount, removeCount);
            for (var r = 0; r < count; ++r) {
                for (int i = 0; i < _ITERATIONS / 2; ++i) {
                    var d = Tube.DIRS.Random();
                    if (!dirs.HasFlag(d)) continue;
                    
                    dirs &= ~d;
                    --dirsCount;
                    break;
                }
            }
            return dirsCount;
        }

        private TubeDir GetDirsToNeighbors(Vector2Int p) {
            TubeDir dirs = 0;
            foreach (var d in Tube.DIRS) {
                var neighborPos = Tube.GetPos(p, d);
                if (isValidCell(neighborPos)) {
                    var cell = GetCell(neighborPos);
                    if (cell.tube != null && cell.tube.Dirs.HasFlag(Tube.GetOpposite(d))) {
                        dirs |= d;
                    }
                }
            }

            return dirs;
        }

        private TubeCell GetCell(Vector2Int p) {
            return _tubesField[p.x, p.y];
        }
        
        private PathCalcCell GetPath(Vector2Int p) {
            return _pathField[p.x, p.y];
        }

        private bool isValidCell(Vector2Int p) {
            return p.x >= 0 && p.x < _cellsWidth && p.y >= 0 && p.y < _cellsHeight;
        }

        private void CreateTube(TubeCell cell, Vector2 p, TubeDir dirs) {
            cell.tube = CreateTube(p, dirs);
            cell.needTube = false;
            _tubes.Add(cell);
        }

        private TubeFragment CreateTube(Vector2 p, TubeDir dirs) {
            var hasValve = Random.value <= _valveChance;
            var tpl = _tubeTpls
                .Where(t => t.Dirs == dirs)
                .Aggregate((t1, t2) => t2.HasValve == hasValve ? t2 : t1);
            var tube = Instantiate(tpl, tpl.transform.parent, false);
            tube.transform.localPosition = _startPos + (Vector3) p * _tubeScale;
            tube.transform.localScale = Vector3.one * _tubeScale;
            tube.gameObject.SetActive(true);
            return tube;
        }
    }
}