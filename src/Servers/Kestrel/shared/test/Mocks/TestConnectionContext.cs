// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestConnectionContext : ConnectionContext
{
    private readonly List<AbortInvocation> _abortInvocations = new();

    public Action<ConnectionAbortedException> OnAbort { get; set; }

    public int AbortCallCount => _abortInvocations.Count;

    public IReadOnlyList<ConnectionAbortedException> AbortReasons => _abortInvocations.Select(invocation => invocation.Exception).ToArray();

    public IFeatureCollection FeaturesCollection { get; set; } = new FeatureCollection();

    public override string ConnectionId { get; set; } = "TestConnectionId";

    public override IFeatureCollection Features => FeaturesCollection;

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IDuplexPipe Transport { get; set; } = new DuplexPipe(PipeReader.Create(Stream.Null), PipeWriter.Create(Stream.Null));

    public override void Abort(ConnectionAbortedException abortReason)
    {
        _abortInvocations.Add(new AbortInvocation(abortReason));
        OnAbort?.Invoke(abortReason);
    }

    public void Reset()
    {
        OnAbort = null;
        _abortInvocations.Clear();
        FeaturesCollection = new FeatureCollection();
    }

    public void AssertAbortCount(int expectedCount)
    {
        Assert.Equal(expectedCount, _abortInvocations.Count);
        foreach (var invocation in _abortInvocations)
        {
            invocation.Verified = true;
        }
    }

    public void AssertAbortCount(int expectedCount, Func<ConnectionAbortedException, bool> predicate)
    {
        var matchingInvocations = _abortInvocations.Where(invocation => predicate(invocation.Exception)).ToList();
        Assert.Equal(expectedCount, matchingInvocations.Count);
        foreach (var invocation in matchingInvocations)
        {
            invocation.Verified = true;
        }
    }

    public void AssertNoOtherCalls()
    {
        Assert.DoesNotContain(_abortInvocations, invocation => !invocation.Verified);
    }

    private sealed class AbortInvocation
    {
        public AbortInvocation(ConnectionAbortedException exception)
        {
            Exception = exception;
        }

        public ConnectionAbortedException Exception { get; }

        public bool Verified { get; set; }
    }
}
