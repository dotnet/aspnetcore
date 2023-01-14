// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// TODO: Docs
/// </summary>
public interface IPersistentComponentState
{
    /// <summary>
    /// TODO: Docs
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    void PersistAsJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, TValue instance);

    /// <summary>
    /// TODO: Docs
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    bool TryTakeFromJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, [MaybeNullWhen(false)] out TValue? instance);
}
