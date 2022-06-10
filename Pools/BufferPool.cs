using System;
using System.Collections;
using System.Collections.Generic;

namespace NetworkTransport.Pools
{
    /// <summary>
    /// Specific pool realization supporting array like poolable elements.
    /// </summary>
    public sealed class BufferPool<T> where T : class, IPoolableBuffer, new ()
    {
        private readonly Dictionary<int, Queue<T>> _buckets;
        
        public BufferPool()
        {
            _buckets = new Dictionary<int, Queue<T>>();
        }

        public T Get(int length)
        {
            if (length <= 0)
            {
                throw new Exception($"Requested buffer pool length is incorrect: {length}");
            }

            T poolable;

            if (_buckets.ContainsKey(length))
            {
                if (_buckets[length].Count > 0)
                {
                    poolable = _buckets[length].Dequeue();
                }
                else
                {
                    poolable = CreatePoolable(length);
                }

                return poolable;
            }

            poolable = CreatePoolable(length);
            return poolable;
        }

        public void Put(T poolable)
        {
            if (poolable == null)
            {
                return;
            }

            poolable.Recycle();

            int length = poolable.GetBufferLength();

            if (!_buckets.ContainsKey(length))
            {
                _buckets.Add(length, new Queue<T>());
            }

            _buckets[length].Enqueue(poolable);
        }

        private T CreatePoolable(int length)
        {
            T poolable = new T();
            poolable.InitBuffer(length);
            return poolable;
        }
    }
}
