// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// For testing.
/// </summary>
public static class UnusedTypeLoader
{
    /// <summary>
    /// For testing.
    /// </summary>
    public static Type? LoadUnusedType()
    {
        // We're intentionally loading an unreferenced type here.
        // This should return null on a published build.

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        return typeof(UnusedTypeLoader).Assembly.GetType($"Microsoft.AspNetCore.Components.{nameof(UnusedTypeForTrimming)}")!;
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    }
}

internal sealed class UnusedTypeForTrimming
{
    public string UnusedProperty { get; } = "Foo";
}
