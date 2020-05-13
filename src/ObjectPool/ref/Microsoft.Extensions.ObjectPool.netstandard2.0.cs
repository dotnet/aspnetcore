// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ObjectPool
{
    public partial class DefaultObjectPoolProvider : Microsoft.Extensions.ObjectPool.ObjectPoolProvider
    {
        public DefaultObjectPoolProvider() { }
        public int MaximumRetained { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.Extensions.ObjectPool.ObjectPool<T> Create<T>(Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T> policy) { throw null; }
    }
    public partial class DefaultObjectPool<T> : Microsoft.Extensions.ObjectPool.ObjectPool<T> where T : class
    {
        public DefaultObjectPool(Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T> policy) { }
        public DefaultObjectPool(Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T> policy, int maximumRetained) { }
        public override T Get() { throw null; }
        public override void Return(T obj) { }
    }
    public partial class DefaultPooledObjectPolicy<T> : Microsoft.Extensions.ObjectPool.PooledObjectPolicy<T> where T : class, new()
    {
        public DefaultPooledObjectPolicy() { }
        public override T Create() { throw null; }
        public override bool Return(T obj) { throw null; }
    }
    public partial interface IPooledObjectPolicy<T>
    {
        T Create();
        bool Return(T obj);
    }
    public partial class LeakTrackingObjectPoolProvider : Microsoft.Extensions.ObjectPool.ObjectPoolProvider
    {
        public LeakTrackingObjectPoolProvider(Microsoft.Extensions.ObjectPool.ObjectPoolProvider inner) { }
        public override Microsoft.Extensions.ObjectPool.ObjectPool<T> Create<T>(Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T> policy) { throw null; }
    }
    public partial class LeakTrackingObjectPool<T> : Microsoft.Extensions.ObjectPool.ObjectPool<T> where T : class
    {
        public LeakTrackingObjectPool(Microsoft.Extensions.ObjectPool.ObjectPool<T> inner) { }
        public override T Get() { throw null; }
        public override void Return(T obj) { }
    }
    public static partial class ObjectPool
    {
        public static Microsoft.Extensions.ObjectPool.ObjectPool<T> Create<T>(Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T> policy = null) where T : class, new() { throw null; }
    }
    public abstract partial class ObjectPoolProvider
    {
        protected ObjectPoolProvider() { }
        public Microsoft.Extensions.ObjectPool.ObjectPool<T> Create<T>() where T : class, new() { throw null; }
        public abstract Microsoft.Extensions.ObjectPool.ObjectPool<T> Create<T>(Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T> policy) where T : class;
    }
    public static partial class ObjectPoolProviderExtensions
    {
        public static Microsoft.Extensions.ObjectPool.ObjectPool<System.Text.StringBuilder> CreateStringBuilderPool(this Microsoft.Extensions.ObjectPool.ObjectPoolProvider provider) { throw null; }
        public static Microsoft.Extensions.ObjectPool.ObjectPool<System.Text.StringBuilder> CreateStringBuilderPool(this Microsoft.Extensions.ObjectPool.ObjectPoolProvider provider, int initialCapacity, int maximumRetainedCapacity) { throw null; }
    }
    public abstract partial class ObjectPool<T> where T : class
    {
        protected ObjectPool() { }
        public abstract T Get();
        public abstract void Return(T obj);
    }
    public abstract partial class PooledObjectPolicy<T> : Microsoft.Extensions.ObjectPool.IPooledObjectPolicy<T>
    {
        protected PooledObjectPolicy() { }
        public abstract T Create();
        public abstract bool Return(T obj);
    }
    public partial class StringBuilderPooledObjectPolicy : Microsoft.Extensions.ObjectPool.PooledObjectPolicy<System.Text.StringBuilder>
    {
        public StringBuilderPooledObjectPolicy() { }
        public int InitialCapacity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MaximumRetainedCapacity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Text.StringBuilder Create() { throw null; }
        public override bool Return(System.Text.StringBuilder obj) { throw null; }
    }
}
