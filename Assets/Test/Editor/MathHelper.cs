using System;
using System.Diagnostics;
using NUnit.Framework;
using Debug = UnityEngine.Debug;

namespace Test.Editor {
    public class MathHelper {
        
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 3)]
        [TestCase(5, 3)]
        [TestCase(9, 3)]
        [TestCase(12, 3)]
        [TestCase(15, 3)]
        public void TestMedian(int len, int k) {
            var a = new ushort[len];
            var sorted = new ushort[len];
            for (int it = 0; it < k; ++it) {
                for (int i = 0; i < len; ++i)
                    a[i] = (ushort) UnityEngine.Random.Range(1,  2 * len);
                Array.Copy(a, sorted, a.Length);
                var watch = Stopwatch.StartNew();
                
                Array.Sort(sorted);
                var med1 = sorted[len / 2];
                var med2 = sorted[(len - 1) / 2];
                watch.Stop();
                var t1 = watch.ElapsedTicks;
                
                watch = Stopwatch.StartNew();
                var med = Utilities.MathHelper.GetMedian(a);
                watch.Stop();
                var t2 = watch.ElapsedTicks;
                
                Assert.IsTrue(med == med1 || med == med2);
                if (t2 > t1) Debug.LogWarning($"GetMedian ({t2}) slowly then Array.Sort ({t1}) on length = {len}");
            }
        }
    }
}