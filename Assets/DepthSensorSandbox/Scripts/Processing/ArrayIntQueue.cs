using System;

namespace DepthSensorSandbox.Processing {
    public class ArrayIntQueue {
        private int[] _a;
        private int _start;
        private int _end;
        private int _count;
        private int _maxSize;

        public int MaxSize {
            get { return _maxSize; }
            set {
                if (_maxSize != value) {
                    _maxSize = value;
                    Array.Resize(ref _a, value);
                }
            }
        }

        public ArrayIntQueue(int maxSize = 10) {
            MaxSize = maxSize;
            Clear();
        }

        public void Clear() {
            _start = -1;
            _end = 0;
            _count = 0;
        }

        public void Enqueue(int i) {
            _a[_end] = i;
            ++_end;
            _end %= _maxSize;
            ++_count;
        }

        public int Dequeue() {
            --_count;
            ++_start;
            _start %= _maxSize;
            return _a[_start];
        }

        public int GetCount() {
            return _count;
        }
    }
}