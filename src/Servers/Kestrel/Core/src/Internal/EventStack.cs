using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal struct EventStack
    {
        private KeyValuePair<Func<object, Task>, object>[] _array;
        private int _size;

        public EventStack(int size)
        {
            _array = size == 0 ? Array.Empty<KeyValuePair<Func<object, Task>, object>>() : new KeyValuePair<Func<object, Task>, object>[size];
            _size = 0;
        }

        public int Count => _size;

        public bool TryPop(out KeyValuePair<Func<object, Task>, object> result)
        {
            int size = _size - 1;
            var array = _array;

            if ((uint)size >= (uint)array.Length)
            {
                result = default;
                return false;
            }

            _size = size;
            result = array[size];
            array[size] = default;
            return true;
        }

        // Pushes an item to the top of the stack.
        public void Push(KeyValuePair<Func<object, Task>, object> item)
        {
            int size = _size;
            var array = _array;

            if ((uint)size < (uint)array.Length)
            {
                array[size] = item;
                _size = size + 1;
            }
            else
            {
                PushWithResize(item);
            }
        }

        // Non-inline from Stack.Push to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushWithResize(KeyValuePair<Func<object, Task>, object> item)
        {
            Array.Resize(ref _array, Math.Max(2 * _array.Length, 1));
            _array[_size] = item;
            _size++;
        }

        public void Clear()
        {
            _array.AsSpan().Clear();
            _size = 0;
        }
    }
}
