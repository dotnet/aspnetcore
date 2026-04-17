// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The context where the restore operation is taking place.
/// </summary>
public sealed class RestoreContext
{
    private readonly bool _initialValue;
    private readonly bool _lastSnapshot;
    private readonly bool _allowUpdates;

    /// <summary>
    /// Gets a <see cref="RestoreContext"/> that indicates the host is restoring initial values.
    /// </summary>
    public static RestoreContext InitialValue { get; } = new RestoreContext(true, false, false);

    /// <summary>
    /// Gets a <see cref="RestoreContext"/> that indicates the host is restoring the last snapshot
    /// available from the previous time the host was running.
    /// </summary>
    public static RestoreContext LastSnapshot { get; } = new RestoreContext(false, true, false);

    /// <summary>
    /// Gets the <see cref="RestoreContext"/> that indicates the host is providing an external state
    /// update to the current value.
    /// </summary>
    public static RestoreContext ValueUpdate { get; } = new RestoreContext(false, false, true);

    private RestoreContext(bool initialValue, bool lastSnapshot, bool allowUpdates)
    {
        _initialValue = initialValue;
        _lastSnapshot = lastSnapshot;
        _allowUpdates = allowUpdates;
    }

    internal bool ShouldRestore(RestoreOptions options)
    {
        if (_initialValue && !options.RestoreBehavior.HasFlag(RestoreBehavior.SkipInitialValue))
        {
            return true;
        }

        if (_lastSnapshot && !options.RestoreBehavior.HasFlag(RestoreBehavior.SkipLastSnapshot))
        {
            return true;
        }

        if (_allowUpdates && options.AllowUpdates)
        {
            return true;
        }

        return false;
    }
}
