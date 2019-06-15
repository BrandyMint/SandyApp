using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;


namespace Test.Editor {
    public class BackgroundConveyer {
        [Test]
        public void TestAddingTasks() {
            var conv = new BgConveyer.BgConveyer();
            var log = new List<string>();
            AddTasks(conv, log);

            conv.Run(); Thread.Sleep(20); conv.Stop();
            PrintTaskLog(log);
            AssertLess("t1", "t2", log);
            AssertLess("t1", "t3", log);
            AssertLess("t1", "t5", log);
            AssertLess("t2", "t5", log);
            AssertLess("t3", "t6", log);
            Assert.Contains("t4", log);
        }

        private void PrintTaskLog(List<string> log) {
            Console.WriteLine("task log:");
            log.ForEach(s => Console.WriteLine(s));
        }

        private void AddTasks(BgConveyer.BgConveyer conv, List<string> log) {
            AddLogTask(conv, "t2", "t1", log);
            AddLogTask(conv, "t3", "t1", log);
            AddLogTask(conv, "t1", null, log);
            AddLogTask(conv, "t4", null, log);
            AddLogTask(conv, "t5", "t2", log);
            AddLogTask(conv, "t6", "t3", log);
        }

        private void AssertLess(string s1, string s2, List<string> log) {
            Assert.Less(log.FindIndex(s => s == s1),
                log.FindIndex(s => s == s2),
                s1 + " < " + s2);
        }

        private static void AddLogTask(BgConveyer.BgConveyer conv, string name, string after, ICollection<string> log , Action act = null) {
            conv.AddToBG(name, after, Routine(name, log, act));
        }

        private static IEnumerator Routine(string mess, ICollection<string> log, Action act = null) {
            log.Add(mess);
            if (act != null)
                act();
            yield return null;
        }
    }
}