using System;
using DepthSensor.Buffer;
using NUnit.Framework;

namespace Test.Editor {
    public class BufferTest {
        private class TestArrayBuffer : ArrayBuffer<ushort> {
            public TestArrayBuffer(int len) : base(len) { }
            public TestArrayBuffer(ushort[] data) : base(data) { }
        }
        
        [Test]
        public void TestRecreateArrayBuffer() {
            TryRecreateArrayBuffer(new ArrayBuffer<int>(10), 15, 9);
            TryRecreateArrayBuffer(new ArrayBuffer<float>(10), 7, 9);
            TryRecreateArrayBuffer<ushort>(new TestArrayBuffer(10), 23, 9);
        }

        [Test]
        public void TestRecreateBuffer2D() {
            TryRecreateBuffer2D(new Buffer2D<int>(10, 15), 3, 7, 6);
            TryRecreateBuffer2D(new Buffer2D<int>(10, 15), 10, 7, 6);
            TryRecreateBuffer2D(new Buffer2D<int>(10, 15), 7, 15, 6);
            TryRecreateBuffer2D<ushort>(new DepthBuffer(10, 15), 7, 13, 6);
        }
        
        private static void TryRecreateArrayBuffer<T>(ArrayBuffer<T> buf, int len2, T testVal) where T : struct {
            int len1 = buf.length;
            var type = buf.GetType();
            try {
                Assert.IsFalse(AbstractBuffer.ReCreateIfNeed(ref buf, len1));
                TestBufferValid(buf, type, len1, testVal);

                Assert.IsTrue(AbstractBuffer.ReCreateIfNeed(ref buf, len2));
                TestBufferValid(buf, type, len2, testVal);
            }
            finally {
                buf?.Dispose();
            }
        }

        private static void TryRecreateBuffer2D<T>(Buffer2D<T> buf, int w2, int h2, T testVal) where T : struct {
            int w1 = buf.width, h1 = buf.height;
            var type = buf.GetType();
            try {
                Assert.IsFalse(AbstractBuffer2D.ReCreateIfNeed(ref buf, w1, h1));
                TestBufferValid(buf, type, w1, h1, testVal);

                Assert.IsTrue(AbstractBuffer2D.ReCreateIfNeed(ref buf, w2, h2));
                TestBufferValid(buf, type, w2, h2, testVal);
            }
            finally {
                buf?.Dispose();
            }
        }
        
        private static void TestBufferValid<T>(ArrayBuffer<T> buf, Type type, int len, T testVal) where T : struct {
            Assert.AreEqual(type, buf.GetType());
            Assert.AreEqual(len, buf.length);
            buf.data[buf.length - 1] = testVal;
            Assert.AreEqual(testVal, buf.data[buf.length - 1]);
        }

        private static void TestBufferValid<T>(Buffer2D<T> buf, Type type, int w, int h, T testVal) where T : struct {
            Assert.AreEqual(type, buf.GetType());
            Assert.AreEqual(w, buf.width);
            Assert.AreEqual(h, buf.height);
            Assert.AreEqual(w * h, buf.length);
            buf.data[buf.length - 1] = testVal;
            Assert.AreEqual(testVal, buf.data[buf.length - 1]);
        }
    }
}