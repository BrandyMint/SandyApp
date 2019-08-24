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

            protected override object[] GetArgsForCreateSome() {
                return new object[] {};
            }

            public TestBuffer() : this(Counter++, 0) {}
            private TestBuffer(int id, int len) : this(len) {
                this.id = id;
            }
            private TestBuffer(int len) : base(len) { }
            private TestBuffer(int[] data) : base(data) { }
        }

        private class TestSensor : Sensor<TestBuffer> {
            public TestSensor() : base(new TestBuffer()) { }
            public TestSensor(bool available) : base(available) { }
            public new class Internal : Sensor<TestBuffer>.Internal {
                private readonly TestSensor _sensor;
                private readonly List<int> _lastBuffers = new List<int>();
                
                protected internal Internal(TestSensor sensor) : base(sensor) {
                    _sensor = sensor;
                }
                
                public new void OnNewFrameBackground() {
                    _lastBuffers.Clear();
                    for (int i = 0; i < _sensor.BuffersValid; ++i) {
                        _lastBuffers.Add(_sensor.Get(i).id);
                    }
                    
                    base.OnNewFrameBackground();
                }

                public void AssertBuffersQueue() {
                    if (_sensor.BuffersValid < 1) return;
                    
                    Debug.Log("========Validate buffers Queue========");
                    PrintBuffers();
                    for (int i = 0; i < _sensor.BuffersValid; ++i) {
                        var valTesting = _sensor.Get(i).id;
                        if (i > 0) {
                            var valNeed = _lastBuffers[i - 1];
                            Debug.Log(valNeed + "\t" + valTesting);
                            Assert.AreEqual(valNeed, valTesting, $"wrong buffer at {i}");
                        }

                        for (int j = i + 1; j < _sensor.BuffersValid; j++) {
                            if (_sensor.Get(j).id == valTesting) 
                                Assert.Fail($"buffers duplicates at {i} and {j} ");
                        }
                    }
                }

                public void PrintBuffers() {
                    Debug.Log($"Buffers count in use: {_sensor.BuffersCount}, first: {_sensor._first}");
                    Debug.Log("testing: " + string.Join(", ", _sensor._buffers.Select(b => b.id)));
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
                    for (int i = 0; i < iterations; ++i) {
                        intern.AssertBuffersQueue();
                        intern.OnNewFrameBackground();
                    }
                }
            }
        }
    }
}