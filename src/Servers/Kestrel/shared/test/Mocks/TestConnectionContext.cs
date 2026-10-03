// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestConnectionContext : ConnectionContext
{
    private readonly List<ConnectionAbortedException> _abortReasons = new();

    public Action<ConnectionAbortedException> OnAbort { get; set; }

    public int AbortCallCount => _abortReasons.Count;

    public IReadOnlyList<ConnectionAbortedException> AbortReasons => _abortReasons;

    public IFeatureCollection FeaturesCollection { get; set; } = new FeatureCollection();

    public override string ConnectionId { get; set; } = "TestConnectionId";

    public override IFeatureCollection Features => FeaturesCollection;

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IDuplexPipe Transport { get; set; } = new DuplexPipe(PipeReader.Create(Stream.Null), PipeWriter.Create(Stream.Null));

    public override void Abort(ConnectionAbortedException abortReason)
    {
        _abortReasons.Add(abortReason);
        OnAbort?.Invoke(abortReason);
    }

    public void Reset()
    {
        OnAbort = null;
        _abortReasons.Clear();
        FeaturesCollection = new FeatureCollection();
    }
}
