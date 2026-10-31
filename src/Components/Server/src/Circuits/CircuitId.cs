// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Consists of a secret (data protected payload) and a non-secret identifier
// for use in logs and user code.
//
// The contract of this is that the id is derived from the Secret. We use the secret
// for comparisons, but we use the id for display (and exposing to user code). As a result,
// we don't include the id in any comparisons done by this class.
//
// Intentionally not overriding ToString here so that this won't accidentally
// get logged. It's ok to log the secret at TRACE.
internal readonly struct CircuitId : IEquatable<CircuitId>
{
    public CircuitId(string secret, string id)
    {
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public string Id { get; }

    public string Secret { get; }

    public bool Equals(CircuitId other)
    {
        // We want to use a fixed time equality comparison for a *real* comparisons.
        // The only use case for Secret being null is with a default struct value,
        // which wouldn't be the result of untrusted input.
        if (other.Secret == null)
        {
            return Secret == null;
        }

        return
            CryptographicOperations.FixedTimeEquals(
                MemoryMarshal.AsBytes(Secret.AsSpan()),
                MemoryMarshal.AsBytes(other.Secret.AsSpan()));
    }

    public override bool Equals(object obj)
    {
        return obj is CircuitId other ? Equals(other) : false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Secret);
    }

    public override string ToString()
    {
        return Id;
    }
}
