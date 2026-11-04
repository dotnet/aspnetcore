// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace BlazorMcpProof;

public sealed class McpFormEffects
{
    private readonly ConcurrentDictionary<string, Observation> _observations = new();

    public void Record(string caseId, string value)
    {
        var state = Get(caseId);
        lock (state)
        {
            state.Count++;
            state.Value = value;
        }
    }

    public Observation Read(string caseId)
    {
        var state = Get(caseId);
        lock (state)
        {
            return new Observation
            {
                Count = state.Count,
                Value = state.Value,
                ComponentsCreated = state.ComponentsCreated,
                ComponentsDisposed = state.ComponentsDisposed,
                ScopesDisposed = state.ScopesDisposed,
            };
        }
    }

    public void ComponentCreated(string caseId) => Interlocked.Increment(ref Get(caseId).ComponentsCreated);
    public void ComponentDisposed(string caseId) => Interlocked.Increment(ref Get(caseId).ComponentsDisposed);
    public void ScopeDisposed(string caseId) => Interlocked.Increment(ref Get(caseId).ScopesDisposed);
    public void Reset(string caseId) => _observations.TryRemove(caseId, out _);
    private Observation Get(string caseId) => _observations.GetOrAdd(caseId, _ => new());

    public sealed class Observation
    {
        public int Count;
        public string Value { get; set; } = "";
        public int ComponentsCreated;
        public int ComponentsDisposed;
        public int ScopesDisposed;
    }
}

public sealed class McpScopeProbe(McpFormEffects effects) : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? CaseId { get; set; }

    public void Dispose()
    {
        if (CaseId is { } caseId)
        {
            effects.ScopeDisposed(caseId);
        }
    }
}
