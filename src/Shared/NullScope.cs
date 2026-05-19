// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Internal;

/// <summary>
/// An empty scope without any logic
/// </summary>
internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new NullScope();

    private NullScope()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
