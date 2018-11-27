// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax
{
    internal class SyntaxListPool
    {
        private ArrayElement<SyntaxListBuilder>[] _freeList = new ArrayElement<SyntaxListBuilder>[10];
        private int _freeIndex;

#if DEBUG
        private readonly List<SyntaxListBuilder> _allocated = new List<SyntaxListBuilder>();
#endif

        internal SyntaxListPool()
        {
        }

        internal SyntaxListBuilder Allocate()
        {
            SyntaxListBuilder item;
            if (_freeIndex > 0)
            {
                _freeIndex--;
                item = _freeList[_freeIndex].Value;
                _freeList[_freeIndex].Value = null;
            }
            else
            {
                item = new SyntaxListBuilder(10);
            }

#if DEBUG
            Debug.Assert(!_allocated.Contains(item));
            _allocated.Add(item);
#endif
            return item;
        }

        internal PooledResult<TNode> Allocate<TNode>() where TNode : GreenNode
        {
            var builder = new SyntaxListBuilder<TNode>(this.Allocate());
            return new PooledResult<TNode>(this, builder);
        }

        internal void Free(SyntaxListBuilder item)
        {
            item.Clear();
            if (_freeIndex >= _freeList.Length)
            {
                this.Grow();
            }
#if DEBUG
            Debug.Assert(_allocated.Contains(item));

            _allocated.Remove(item);
#endif
            _freeList[_freeIndex].Value = item;
            _freeIndex++;
        }

        private void Grow()
        {
            var tmp = new ArrayElement<SyntaxListBuilder>[_freeList.Length * 2];
            Array.Copy(_freeList, tmp, _freeList.Length);
            _freeList = tmp;
        }

        public SyntaxList<TNode> ToListAndFree<TNode>(SyntaxListBuilder<TNode> item)
            where TNode : GreenNode
        {
            var list = item.ToList();
            Free(item);
            return list;
        }

        public readonly struct PooledResult<TNode> : IDisposable where TNode : GreenNode
        {
            private readonly SyntaxListBuilder<TNode> _builder;
            private readonly SyntaxListPool _pool;

            public PooledResult(SyntaxListPool pool, in SyntaxListBuilder<TNode> builder)
            {
                _pool = pool;
                _builder = builder;
            }

            public SyntaxListBuilder<TNode> Builder => _builder;

            public void Dispose()
            {
                _pool.Free(_builder);
            }
        }
    }
}
