// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

// Focused allocation benchmark for conventional (non-attribute) action selection. Each op performs a
// single ActionSelector.SelectCandidates call against a pre-built RouteContext, so the only per-op
// allocation is the lookup key materialized inside ActionSelectionTable.Select (the matched candidate
// list returned on a hit is the shared cached instance, not a copy). This isolates the string[] key
// allocation being eliminated, across a few/many route-value counts and exact/ignore-case/no-match
// lookups (ordinal fast path vs. ordinal-ignore-case fallback).
public class ConventionalActionSelectionBenchmark
{
    private static readonly string[] _keyNames =
    {
        "controller", "action", "area", "segment3", "segment4", "segment5", "segment6", "segment7",
    };

    private static readonly string[] _values =
    {
        "Home", "Index", "Admin", "v3", "v4", "v5", "v6", "v7",
    };

    [Params(1, 3, 8)]
    public int RouteValueCount { get; set; }

    private IActionSelector _actionSelector = default!;
    private RouteContext _exactMatchContext = default!;
    private RouteContext _ignoreCaseMatchContext = default!;
    private RouteContext _noMatchContext = default!;

    [GlobalSetup]
    public void Setup()
    {
        var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < RouteValueCount; i++)
        {
            canonical.Add(_keyNames[i], _values[i]);
        }

        var action = new ActionDescriptor { RouteValues = canonical };
        _actionSelector = CreateActionSelector(new[] { action });

        _exactMatchContext = CreateContext(canonical, transform: static v => v);
        _ignoreCaseMatchContext = CreateContext(canonical, transform: static v => v.ToLowerInvariant());
        _noMatchContext = CreateContext(canonical, transform: static v => v + "-nomatch");
    }

    [Benchmark(Description = "exact-case match (ordinal fast path)")]
    public IReadOnlyList<ActionDescriptor> ExactMatch()
        => _actionSelector.SelectCandidates(_exactMatchContext);

    [Benchmark(Description = "different-case match (ordinal-ignore-case fallback)")]
    public IReadOnlyList<ActionDescriptor> IgnoreCaseMatch()
        => _actionSelector.SelectCandidates(_ignoreCaseMatchContext);

    [Benchmark(Description = "no match")]
    public IReadOnlyList<ActionDescriptor> NoMatch()
        => _actionSelector.SelectCandidates(_noMatchContext);

    private static RouteContext CreateContext(Dictionary<string, string> canonical, Func<string, string> transform)
    {
        var context = new RouteContext(new DefaultHttpContext());
        foreach (var kvp in canonical)
        {
            context.RouteData.Values[kvp.Key] = transform(kvp.Value);
        }

        return context;
    }

    private static IActionSelector CreateActionSelector(ActionDescriptor[] actions)
    {
        var actionCollection = new MockActionDescriptorCollectionProvider(actions);

        return new ActionSelector(
            actionCollection,
            new ActionConstraintCache(actionCollection, Enumerable.Empty<IActionConstraintProvider>()),
            NullLoggerFactory.Instance);
    }

    private sealed class MockActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
    {
        public MockActionDescriptorCollectionProvider(ActionDescriptor[] actions)
        {
            ActionDescriptors = new ActionDescriptorCollection(actions, 0);
        }

        public ActionDescriptorCollection ActionDescriptors { get; }
    }
}
