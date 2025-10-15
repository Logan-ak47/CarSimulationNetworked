using System.Threading;

namespace CarSim.Shared
{
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _capacity;
        private int _head;
        private int _tail;
        private int _count;
        private readonly object _lock = new object();

        public RingBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public bool TryEnqueue(T item)
        {
            lock (_lock)
            {
                if (_count >= _capacity)
                {
                    return false; // Buffer full
                }

                _buffer[_tail] = item;
                _tail = (_tail + 1) % _capacity;
                _count++;
                return true;
            }
        }

        public bool TryDequeue(out T item)
        {
            lock (_lock)
            {
                if (_count == 0)
                {
                    item = default(T);
                    return false;
                }

                item = _buffer[_head];
                _buffer[_head] = default(T);
                _head = (_head + 1) % _capacity;
                _count--;
                return true;
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _count;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _head = 0;
                _tail = 0;
                _count = 0;
                for (int i = 0; i < _capacity; i++)
                {
                    _buffer[i] = default(T);
                }
            }
        }
    }
}
