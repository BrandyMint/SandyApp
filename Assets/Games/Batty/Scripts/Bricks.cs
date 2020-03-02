using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Games.Common;
using Games.Common.GameFindObject;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Games.Batty {
    public class Bricks : MonoBehaviour {
        [SerializeField] private InteractableSimple _tplBrick;
        [TextArea] [SerializeField] private string _map;

        private string[] _mapLines;
        private readonly List<InteractableSimple> _bricks = new List<InteractableSimple>();
        private readonly Dictionary<int, Color> _colors = new Dictionary<int, Color>();

        private void Awake() {
            _tplBrick.gameObject.SetActive(false);
            _mapLines = Regex.Split(_map, "\r\n|\r|\n");
            var first = _mapLines.First();
            Assert.IsTrue(_mapLines.All(l => l.Length == first.Length), "Check Bricks map");
        }

        public int Count => _bricks.Count;

        public void Show(bool show) {
             ClearBricks();
             if (show)
                 SpawnBricks();
        }

        private void ClearBricks() {
            foreach (var brick in _bricks) {
                if (brick != null)
                    Destroy(brick.gameObject);
            }
            _bricks.Clear();
            _colors.Clear();
        } 

        private void SpawnBricks() {
            var p = int2.zero;
            var size = new float3(
                1f / _mapLines.First().Length, 
                1f / _mapLines.Length,
                _tplBrick.transform.localScale.z 
            );
            for (p.y = 0; p.y < _mapLines.Length; ++p.y) {
                var line = _mapLines[_mapLines.Length - p.y - 1];
                for (p.x = 0; p.x < line.Length; ++p.x) {
                    var type = int.Parse(line[p.x].ToString());
                    if (type != 0) {
                        var brick = Instantiate(_tplBrick, _tplBrick.transform.parent, false);
                        brick.transform.localScale = size;
                        brick.transform.localPosition = new float3(size.xy * p + size.xy / 2f, 0f) - new float3(0.5f, 0.5f, 0f);
                        brick.ItemType = type;
                        SetColor(brick.GetComponentInChildren<RandomColorRenderer>(), type);
                        brick.gameObject.SetActive(true);
                        _bricks.Add(brick);
                    }
                }
            }
        }

        private void SetColor(RandomColorRenderer colorRend, int type) {
            if (_colors.TryGetValue(type, out var color)) {
                colorRend.SetColor(color);
            } else {
                colorRend.SetRandomColor();
                _colors[type] = colorRend.GetColor();
            }
        }
    }
}