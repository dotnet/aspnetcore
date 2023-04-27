// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Components.Endpoints;

// Used to produce a monotonically increasing sequence starting at 0 that is unique for the scope of the top-level page/view/component being rendered.
internal sealed class ServerComponentInvocationSequence
{
    private int _sequence;

    public ServerComponentInvocationSequence()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        Value = new Guid(bytes);
        _sequence = -1;
    }

    public Guid Value { get; }

    public int Next() => ++_sequence;
}
