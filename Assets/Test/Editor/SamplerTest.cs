using System;
using DepthSensorSandbox.Processing;
using NUnit.Framework;
using UnityEngine;

namespace Test.Editor {
    public class SamplerTest {
        //  7 0 4
        //  3 i 1
        //  6 2 5
        private Vector2Int[] _neighbours = new[] {
            new Vector2Int(0, 1),    //0
            new Vector2Int(1, 0),    //1
            new Vector2Int(0, -1),   //2
            new Vector2Int(-1, 0),   //3
            new Vector2Int(1, 1),    //4
            new Vector2Int(1, -1),   //5
            new Vector2Int(-1, -1),  //6
            new Vector2Int(-1, 1)    //7
        }; 
        
        [TestCase(1, 1)]
        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        [TestCase(4, 2)]
        [TestCase(16, 16)]
        [TestCase(9, 16)]
        [TestCase(16, 9)]
        public void TestGetNeighbors(int w, int h) {
            var s = Sampler.Create();
            var indexes = new int [w, h];
            var i = 0;
            for (var y = 0; y < h; ++y) {
                for (var x = 0; x < w; ++x) {
                    indexes[x, y] = i++;
                }
            }

            s.SetDimens(w, h);
            var p = Vector2Int.zero;
            for (p.x = 0; p.x < w; ++p.x) {
                for (p.y = 0; p.y < h; ++p.y) {
                    var id = GetIdFromIndexes(indexes, p);
                    if (id < 0) continue;
                    for (int k = 0; k < _neighbours.Length; ++k) {
                        var pn = p + _neighbours[k];
                        var idn = GetIdFromIndexes(indexes, pn);
                        Assert.AreEqual(idn, s.GetIndexOfNeighbor(id, k), $"i = {id}, k = {k}, pn = {pn}");
                    }
                }
            }
        }
        
        [TestCase(1, 1, 0, 0, 1, 1)]
        [TestCase(1, 3, 0, 1, 1, 2)]
        [TestCase(2, 3, 1, 0, 1, 2)]
        [TestCase(1, 4, 0, 1, 1, 3)]
        [TestCase(2, 4, 1, 0, 1, 3)]
        [TestCase(3, 1, 2, 0, 3, 0)]
        [TestCase(3, 2, -1, 0, 2, 1)]
        [TestCase(4, 1, 2, 3, 0, 0)]
        [TestCase(4, 2, 2, 3, 0, 1)]
        [TestCase(16, 16, 5, 4, 13, 15)]
        [TestCase(9, 16, 3, 5, 8, 13)]
        [TestCase(16, 9, 5, 3, 16, 9)]
        public void TestGetNeighborsWithCropping(int w, int h, int xMin, int yMin, int xMax, int yMax) {
            var s = Sampler.Create();
            var r = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            var indexes = new int [w, h];
            var i = 0;
            for (var y = 0; y < h; ++y) {
                for (var x = 0; x < w; ++x) {
                    indexes[x, y] = i++;
                }
            }

            s.SetDimens(w, h);
            s.SetCropping(r);
            var p = Vector2Int.zero;
            for (p.x = 0; p.x < w; ++p.x) {
                for (p.y = 0; p.y < h; ++p.y) {
                    var id = GetIdFromIndexes(indexes, p, r);
                    if (id < 0) continue;
                    for (int k = 0; k < _neighbours.Length; ++k) {
                        var pn = p + _neighbours[k];
                        var idn = GetIdFromIndexes(indexes, pn, r);
                        Assert.AreEqual(idn, s.GetIndexOfNeighbor(id, k), $"i = {id}, k = {k}, pn = {pn}");
                    }
                }
            }
        }

        private int GetIdFromIndexes(int[,] indexes, Vector2Int p, RectInt cropping) {
            if (cropping.Contains(p))
                return GetIdFromIndexes(indexes, p);
            return Sampler.INVALID_ID;
        }

        private static int GetIdFromIndexes(int[,] indexes, Vector2Int p) {
            try {
                return indexes[p.x, p.y];
            }
            catch (IndexOutOfRangeException) {
                return Sampler.INVALID_ID;
            }
        }
    }
}