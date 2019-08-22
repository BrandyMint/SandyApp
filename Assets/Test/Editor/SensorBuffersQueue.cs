using System;
using System.Collections.Generic;
using System.Linq;
using DepthSensor.Buffer;
using DepthSensor.Sensor;
using NUnit.Framework;
using UnityEngine;

namespace Test.Editor {
    public class SensorBuffersQueue {
        private class TestBuffer : ArrayBuffer<int> {
            public static int Counter;
            
            public readonly int id; 

            public override AbstractBuffer CreateSome() {
                return new TestBuffer();
            }
            
            public TestBuffer() : this(Counter++, 0) {}
            private TestBuffer(int id, int len) : this(len) {
                this.id = id;
            }
            private TestBuffer(int len) : base(len) { }
            private TestBuffer(int[] data) : base(data) { }
        }

        private class TestSensor : Sensor<TestBuffer> {
            protected List<int> _list = new List<int> {0};
            public TestSensor() : base(new TestBuffer()) { }
            public TestSensor(bool available) : base(available) { }
            public new class Internal : Sensor<TestBuffer>.Internal {
                private readonly TestSensor _sensor;
                private int _validCount;
                private int _counter = 1;
                protected internal Internal(TestSensor sensor) : base(sensor) {
                    _sensor = sensor;
                }

                private int GetOldestIdx() {
                    UpdateTestBuffersCount();
                    return _sensor.BuffersCount - 1;
                }

                private void UpdateTestBuffersCount() {
                    while (_sensor._list.Count < _sensor.BuffersCount) {
                        _sensor._list.Add( _counter++);
                    }
                }

                private void TestBufferNewFrame() {
                    var idxLast = GetOldestIdx();
                    var id = _sensor._list[idxLast];
                    _sensor._list.RemoveAt(idxLast);
                    _sensor._list.Insert(0, id);
                    _validCount = Math.Min(++_validCount, _sensor.BuffersCount);
                }
                
                public new void OnNewFrameBackground() {
                    UpdateTestBuffersCount();
                    TestBufferNewFrame();
                    base.OnNewFrameBackground();
                }

                public void AssertBuffersQueue() {
                    if (_sensor.BuffersValid < 1) return;
                    
                    Debug.Log("========Validate buffers Queue========");
                    for (int i = 0; i < _sensor.BuffersValid; ++i) {
                        var valNeed = _sensor._list[i];
                        var valTesting = _sensor.Get(i).id;
                        Debug.Log(valNeed + "\t" + valTesting);
                        Assert.AreEqual(valNeed, valTesting, $"wrong buffer at {i}");
                    }
                    Assert.AreEqual(_sensor._list[0], _sensor.GetNewest().id, "wrong newest");
                    Assert.AreEqual(_sensor._list[GetOldestIdx()], _sensor.GetOldest().id, "wrong oldest");
                }

                public void AssertBufferValidCount() {
                    Assert.AreEqual(_validCount, _sensor.BuffersValid, "wrong valid buffers count");
                }

                public void PrintBuffers() {
                    Debug.Log($"Buffers count in use: {_sensor.BuffersCount}, first: {_sensor._first}");
                    Debug.Log("testing: " + string.Join(", ", _sensor._buffers.Select(b => b.id)));
                    Debug.Log("control: " + string.Join(", ", _sensor._list));
                }
            }
        }

        [SetUp]
        public void SetUp() {
            TestBuffer.Counter = 0;
        }
        
        [Test]
        public void SingleBufferSimple() {
            using (var sensor = new TestSensor()) {
                Assert.AreEqual(sensor.BuffersCount, 1);
                Assert.NotNull(sensor.GetNewest());
                Assert.AreSame(sensor.GetNewest(), sensor.GetOldest());
                Assert.AreSame(sensor.GetNewest(), sensor.Get(0));
                Assert.AreSame(sensor.GetNewest(), sensor.Get(1));
                Assert.AreSame(sensor.GetNewest(), sensor.Get(2));
            }
        }
        
        [Test]
        public void SingleBufferValid() {
            using (var sensor = new TestSensor()) {
                var intern = new TestSensor.Internal(sensor);
                var buffNew = sensor.GetNewest();
                var buffOld = sensor.GetOldest();
                Assert.NotNull(buffNew);
                Assert.NotNull(buffOld);
                Assert.AreEqual(0, sensor.BuffersValid);

                for (int i = 0; i < 5; ++i) {
                    intern.OnNewFrameBackground();
                    Assert.AreEqual(sensor.BuffersValid, 1);
                    Assert.AreEqual(sensor.BuffersCount, 1);
                    Assert.AreSame(buffNew, sensor.GetNewest());
                    Assert.AreSame(buffOld, sensor.GetOldest());
                }
            }
        }

        [TestCase(10, new int[] {2})]
        [TestCase(10, new int[] {3})]
        [TestCase(10, new int[] {5})]
        [TestCase(10, new int[] {2, 5, 3, 1, 3})]
        public void MultipleBuffer(int iterations, int[] bufferCounts) {
            using (var sensor = new TestSensor()) {
                var intern = new TestSensor.Internal(sensor);

                foreach (var bufferCount in bufferCounts) {
                    sensor.BuffersCount = bufferCount;
                    intern.PrintBuffers();
                    for (int i = 0; i < iterations; ++i) {
                        intern.AssertBufferValidCount();
                        intern.AssertBuffersQueue();
                        intern.OnNewFrameBackground();
                    }
                }
            }
        }
    }
}