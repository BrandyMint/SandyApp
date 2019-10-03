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
            var s = new Sampler();
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
                    
                    for (int k = 0; k < _neighbours.Length; ++k) {
                        var pn = p + _neighbours[k];
                        var idn = GetIdFromIndexes(indexes, pn);
                        Assert.AreEqual(idn, s.GetIndexOfNeighbor(id, k), $"i = {id}, k = {k}, pn = {pn}");
                    }
                }
            }
        }

        private static int GetIdFromIndexes(int[,] indexes, Vector2Int i) {
            try {
                return indexes[i.x, i.y];
            }
            catch (IndexOutOfRangeException) {
                return Sampler.INVALID_ID;
            }
        }
    }
}