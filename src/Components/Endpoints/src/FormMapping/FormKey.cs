// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal readonly struct FormKey(ReadOnlyMemory<char> value) : IEquatable<FormKey>
{
    private readonly int _hashCode = string.GetHashCode(value.Span, StringComparison.OrdinalIgnoreCase);

    public ReadOnlyMemory<char> Value { get; } = value;

    public override readonly bool Equals(object? obj) => obj is FormKey prefix && Value.Equals(prefix.Value);

    public readonly bool Equals(FormKey other) =>
        MemoryExtensions.Equals(Value.Span, other.Value.Span, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => _hashCode;

    private string GetDebuggerDisplay() => Value.ToString();
}
