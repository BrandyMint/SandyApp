using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace UINotify {
    public class Notify : MonoBehaviour {
        public class Control {
            public bool IsAlive { get; private set; } = true;

            public void Set(Params p) {
                Instance.ExecuteOrEnqueue(new Task {type = TaskType.SET, param = p, target = this});
            }
            public void Hide() {
                Instance.ExecuteOrEnqueue(new Task {type = TaskType.HIDE, target = this});
            }

            public Control() {
                NotifyController.OnFinish += (c) => {
                    if (c == this) IsAlive = false;
                };
            }
        }
        
        public struct Params {
            public Style style;
            public LifeTime time;
            public string title;
            public string text;
            public GameObject content;
        }

        private enum TaskType {
            CREATE,
            SET,
            HIDE
        }

        private class Task {
            public TaskType type;
            public Params param;
            public Control target;
        }

        private static Notify _instance;
        public static Notify Instance {
            get {
                if (_instance == null) {
                    _instance = new GameObject("Notify").AddComponent<Notify>();
                }
                return _instance;
            }
        }

        private NotifyController _controller;
        private readonly Queue<Task> _tasksQueue = new Queue<Task>();

        private void Awake() {
            _controller = Resources.Load<NotifyController>("Prefabs/Notify");
            _controller = Instantiate(_controller, transform, false);
            DontDestroyOnLoad(gameObject);
        }

        private void FixedUpdate() {
            lock (Instance._tasksQueue) {
                while (_tasksQueue.Any()) {
                    var task = _tasksQueue.Dequeue();
                    ExecuteTask(task);
                }
            }
        }

        private void ExecuteOrEnqueue(Task task) {
            if (Thread.CurrentThread.IsBackground) {
                lock (_tasksQueue) {
                    _tasksQueue.Enqueue(task);
                }
            } else {
                ExecuteTask(task);
            }
        }

        private void ExecuteTask(Task task) {
            if (task.type == TaskType.HIDE) {
                _controller.Hide(task.target);
                return;
            }

            if (task.type == TaskType.CREATE) {
                _controller.CreateNew(task.target);
            }
            if (task.type == TaskType.SET || task.type == TaskType.CREATE) {
                _controller.Set(task.target, task.param);
            }
        }

        public static Control Show(Params p) {
            var control = new Control();
            var task = new Task {
                type = TaskType.CREATE,
                param = p,
                target = control
            };
            Instance.ExecuteOrEnqueue(task);
            return control;
        }

        public static Control Show(Style style, string text) {
            return Show(new Params {
                style = style,
                time = LifeTime.SHORT,
                text = text
            });
        }

        public static Control Show(Style style, string title, string text) {
            return Show(new Params {
                style = style,
                time = LifeTime.SHORT,
                title = title,
                text = text
            });
        }
        
        public static Control Show(Style style, LifeTime time, string text) {
            return Show(new Params {
                style = style,
                time = time,
                text = text
            });
        }

        public static Control Show(Style style, LifeTime time, string title, string text) {
            return Show(new Params {
                style = style,
                time = time,
                title = title,
                text = text
            });
        }
    }
}