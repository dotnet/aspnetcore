// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Xunit;

namespace Microsoft.Extensions.ObjectPool
{
    public class ThreadingTest
    {
        private CancellationTokenSource _cts;
        private DefaultObjectPool<Item> _pool;
        private bool _foundError;

        [Fact]
        public void DefaultObjectPool_RunThreadingTest()
        {
            _pool = new DefaultObjectPool<Item>(new DefaultPooledObjectPolicy<Item>(), 10);
            RunThreadingTest();
        }

        [Fact]
        public void DisposableObjectPool_RunThreadingTest()
        {
            _pool = new DisposableObjectPool<Item>(new DefaultPooledObjectPolicy<Item>(), 10);
            RunThreadingTest();
        }

        private void RunThreadingTest()
        {
            _cts = new CancellationTokenSource();

            var threads = new Thread[8];
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(Run);
            }

            for (var i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }

            // Run for 1000ms
            _cts.CancelAfter(1000);

            // Wait for all threads to complete
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            Assert.False(_foundError, "Race condition found. An item was shared across threads.");
        }

        private void Run()
        {
            while (!_cts.IsCancellationRequested)
            {
                var obj = _pool.Get();
                if (obj.i != 0)
                {
                    _foundError = true;
                }
                obj.i = 123;

                var obj2 = _pool.Get();
                if (obj2.i != 0)
                {
                    _foundError = true;
                }
                obj2.i = 321;

                obj.Reset();
                _pool.Return(obj);

                obj2.Reset();
                _pool.Return(obj2);
            }
        }

        private class Item
        {
            public int i = 0;

            public void Reset()
            {
                i = 0;
            }
        }
    }
}
