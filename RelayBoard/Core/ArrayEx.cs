using System;
using System.Runtime.CompilerServices;

namespace RelayBoard.Core
{
    public class ArrayEx<T>
    {
        private T[] _array;
        private int _capacity, _index;

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _array[index] = value;
        }

        public ArrayEx(int capacity = 16)
        {
            _array = new T[capacity];
            _capacity = capacity;
            _index = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureCapacity();
            _array[_index++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCapacity(int capacity)
        {
            _capacity = capacity;
            var copy = new T[capacity];
            Array.Copy(_array, copy, Math.Min(_array.Length, _capacity));
            _array = copy;
            _index = Math.Min(capacity, _index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _index = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity()
        {
            if (_index < _capacity) return;

            while (_capacity < _index)
                _capacity *= 2;
            var copy = new T[_capacity];
            Array.Copy(_array, copy, _array.Length);
            _array = copy;
        }
    }
}
