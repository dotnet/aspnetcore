// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public interface IFormStateProvider
{
    public string? Handler { get; }

    public bool IsAvailable { get; }

    public IReadOnlyDictionary<string, string?> Fields { get; }
}
