using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

namespace BgConveyer {
    public class BgConveyer : MonoBehaviour {
        public class ToNewIteration { }
        
        protected List<TaskInfo> tasks = new List<TaskInfo>();

        protected class TaskInfo {
            public int id = -1;
            public string name;
            public string after;
            public IEnumerator action;
            public TaskType type;
            public int removeId;
        }

        protected enum TaskType {
            BG,
            MAIN_THREAD,
            REMOVE
        }

        protected string _defaultAfter = null;

        private Thread thread = null;
        private List<TaskInfo> currMainThreadTasks = new List<TaskInfo>();
        private List<TaskInfo> watingChangeTasks = new List<TaskInfo>();
        private volatile bool loop = false;
        private int _idCounter;

        private void Update() {
            if (currMainThreadTasks.Count > 0 && Monitor.TryEnter(currMainThreadTasks)) {
                try {
                    if (loop) {
                        foreach (var task in currMainThreadTasks) {
                            DoAction(task);
                        }
                    }
                    currMainThreadTasks.Clear();
                } finally {
                    Monitor.Exit(currMainThreadTasks);
                }
            }
        }

        protected void OnDestroy() {
            Stop();
            if (thread != null && thread.IsAlive && !thread.Join(1000)) {
                loop = false;
                thread.Abort();
            }
        }

        public void Run() {
            if (thread == null) {
                thread = new Thread(Loop) {Name = GetType().Name};
            }
            loop = true;
            if (!thread.IsAlive)
                thread.Start();
        }

        public void Stop() {
            loop = false;
        }

        private void Loop() {
            try {
                while (loop) {
                    AcceptChangesTasks();
                    foreach (var task in tasks) {
                        if (task.type == TaskType.BG) {
                            if (!DoAction(task)) break;
                        } else {
                            Monitor.Enter(currMainThreadTasks);
                            if (!currMainThreadTasks.Contains(task)) {
                                currMainThreadTasks.Add(task);
                            }
                            Monitor.Exit(currMainThreadTasks);
                        }
                    }
                }
            }
            catch (ThreadAbortException) {
                Debug.Log(GetType().Name + ": Thread aborted");
            } catch (Exception e) {
                Debug.LogException(e);
                throw;
            } finally {
                if (Monitor.TryEnter(currMainThreadTasks))
                    Monitor.Exit(currMainThreadTasks);
            }
        }

        private void AcceptChangesTasks() {
            Monitor.Enter(watingChangeTasks);
            watingChangeTasks.Sort((t1, t2) =>
                (t1.after == null || t1.name == t2.after) ? -1 : 1
            );
            int i = 0;
            while (i < watingChangeTasks.Count) {
                var changeTask = watingChangeTasks[i];
                var removeChangeTask = true;
                if (changeTask.type == TaskType.REMOVE) {
                    removeChangeTask = RemoveDependedTasks(tasks, changeTask.removeId);
                } else {
                    tasks.RemoveAll(task => task.name == changeTask.name);
                    if (changeTask.after == null) {
                        tasks.Insert(tasks.FindLastIndex(task => task.after == null) + 1, changeTask);
                    } else {
                        var insId = tasks.FindLastIndex(task => task.name == changeTask.after);
                        if (insId != -1) {
                            tasks.Insert(insId + 1, changeTask);
                        } else {
                            removeChangeTask = false;
                        }
                    }
                }

                if (removeChangeTask)
                    watingChangeTasks.RemoveAt(i);
                else
                    ++i;
            }
            watingChangeTasks.Clear();
            Monitor.Exit(watingChangeTasks);
        }

        private static bool RemoveDependedTasks(List<TaskInfo> tasks, int id) {
            var toRemove = new List<int>();
            foreach (var task in tasks) {
                if (task.id == id) {
                    foreach (var depend in tasks) {
                        if (depend.after == task.name)
                            toRemove.Add(depend.id);
                    }
                }
            }
            foreach (var dependId in toRemove) {
                RemoveDependedTasks(tasks, dependId);
            }
            return tasks.RemoveAll(info => info.id == id) > 0;
        }

        protected bool DoAction(TaskInfo task) {
            if (task.action.MoveNext()) {
                var res = task.action.Current;
                return !(res is ToNewIteration);
            } else {
                RemoveTask(task.id);
            }

            return true;
        }

        public void RemoveTask(int id) {
            AddWatingChangeTasks(new TaskInfo {
                action = null,
                name = "",
                type = TaskType.REMOVE,
                removeId = id
            });
        }

        public int AddToBG(string name, string after, IEnumerator act) {
            return AddWatingChangeTasks(new TaskInfo {
                action = act,
                name = name,
                after = after,
                type = TaskType.BG
            });
        }

        public int AddToMainThread(string name, string after, IEnumerator act) {
            return AddWatingChangeTasks(new TaskInfo {
                action = act,
                name = name,
                after = after,
                type = TaskType.MAIN_THREAD
            });
        }

        private int AddWatingChangeTasks(TaskInfo info) {
            Assert.IsNotNull(info.name, "Name of task must be not null!");
            if (info.type != TaskType.REMOVE) {
                info.id = _idCounter++;
                if (info.after == null)
                    info.after = _defaultAfter;
            }
            Monitor.Enter(watingChangeTasks);
            watingChangeTasks.Add(info);
            Monitor.Exit(watingChangeTasks);
            return info.id;
        }
    }
}