// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions
{
    public partial class FileHandleEndPoint : System.Net.EndPoint
    {
        public FileHandleEndPoint(ulong fileHandle, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.FileHandleType fileHandleType) { }
        public ulong FileHandle { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.FileHandleType FileHandleType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public enum FileHandleType
    {
        Auto = 0,
        Tcp = 1,
        Pipe = 2,
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public static partial class KestrelMemoryPool
    {
        public static readonly int MinimumSegmentSize;
        public static System.Buffers.MemoryPool<byte> Create() { throw null; }
        public static System.Buffers.MemoryPool<byte> CreateSlabMemoryPool() { throw null; }
    }
    public abstract partial class TransportConnection : Microsoft.AspNetCore.Connections.ConnectionContext, Microsoft.AspNetCore.Connections.Features.IConnectionIdFeature, Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature, Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature, Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature, Microsoft.AspNetCore.Connections.Features.IMemoryPoolFeature, Microsoft.AspNetCore.Http.Features.IFeatureCollection, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        public TransportConnection() { }
        public System.IO.Pipelines.IDuplexPipe Application { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.Threading.CancellationToken ConnectionClosed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
        public System.IO.Pipelines.PipeWriter Input { get { throw null; } }
        public override System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } set { } }
        public override System.Net.EndPoint LocalEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        System.Collections.Generic.IDictionary<object, object> Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature.Items { get { throw null; } set { } }
        System.Threading.CancellationToken Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.ConnectionClosed { get { throw null; } set { } }
        System.IO.Pipelines.IDuplexPipe Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature.Transport { get { throw null; } set { } }
        System.Buffers.MemoryPool<byte> Microsoft.AspNetCore.Connections.Features.IMemoryPoolFeature.MemoryPool { get { throw null; } }
        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get { throw null; } }
        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[System.Type key] { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get { throw null; } }
        public System.IO.Pipelines.PipeReader Output { get { throw null; } }
        public override System.Net.EndPoint RemoteEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.IO.Pipelines.IDuplexPipe Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        void Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.Abort() { }
        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature) { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
