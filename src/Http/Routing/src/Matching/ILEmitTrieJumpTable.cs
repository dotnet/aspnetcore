// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Routing.Matching;

// Uses generated IL to implement the JumpTable contract. This approach requires
// a fallback jump table for two reasons:
// 1. We compute the IL lazily to avoid taking up significant time when processing a request
// 2. The generated IL only supports ASCII in the URL path
[RequiresDynamicCode("ILEmitTrieJumpTable uses runtime IL generation.")]
internal sealed class ILEmitTrieJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly (string text, int destination)[] _entries;

    private readonly bool? _vectorize;
    private readonly JumpTable _fallback;

    // Used to protect the initialization of the compiled delegate
    private object _lock;
    private bool _initializing;
    private Task _task;

    // Will be replaced at runtime by the generated code.
    //
    // Internal for testing
    internal Func<string, PathSegment, int> _getDestination;

    public ILEmitTrieJumpTable(
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries,
        bool? vectorize,
        JumpTable fallback)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
        _entries = entries;
        _vectorize = vectorize;
        _fallback = fallback;

        _getDestination = FallbackGetDestination;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        return _getDestination(path, segment);
    }

    // Used when we haven't yet initialized the IL trie. We defer compilation of the IL for startup
    // performance.
    private int FallbackGetDestination(string path, PathSegment segment)
    {
        if (path.Length == 0)
        {
            return _exitDestination;
        }

        // We only hit this code path if the IL delegate is still initializing.
        LazyInitializer.EnsureInitialized(ref _task, ref _initializing, ref _lock, InitializeILDelegateAsync);

        return _fallback.GetDestination(path, segment);
    }

    // Internal for testing
    internal async Task InitializeILDelegateAsync()
    {
        // Offload the creation of the IL delegate to the thread pool.
        await Task.Run(InitializeILDelegate);
    }

    // Internal for testing
    internal void InitializeILDelegate()
    {
        var generated = ILEmitTrieFactory.Create(_defaultDestination, _exitDestination, _entries, _vectorize);
        _getDestination = (string path, PathSegment segment) =>
        {
            if (segment.Length == 0)
            {
                return _exitDestination;
            }

            var result = generated(path, segment.Start, segment.Length);
            if (result == ILEmitTrieFactory.NotAscii)
            {
                result = _fallback.GetDestination(path, segment);
            }

            return result;
        };
    }
}
