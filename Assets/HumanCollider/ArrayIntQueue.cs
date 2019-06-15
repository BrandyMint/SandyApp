namespace HumanCollider {
    public class ArrayIntQueue {
        private int[] a;
        private int start;
        private int end;
        private int count;
        private int maxSize;

        public int MaxSize {
            get { return maxSize; }
            set {
                maxSize = value;
                a = new int[value];
            }
        }

        public ArrayIntQueue(int maxSize = 10) {
            MaxSize = maxSize;
            Clear();
        }

        public void Clear() {
            start = -1;
            end = 0;
            count = 0;
        }

        public void Enqueue(int i) {
            a[end] = i;
            ++end;
            end %= maxSize;
            ++count;
        }

        public int Dequeue() {
            --count;
            ++start;
            start %= maxSize;
            return a[start];
        }

        public int GetCount() {
            return count;
        }
    }
}