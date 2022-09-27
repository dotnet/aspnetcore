// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// Defines a key/value metadata pair for the decorated Razor type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RazorCompiledItemMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="RazorCompiledItemMetadataAttribute"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public RazorCompiledItemMetadataAttribute(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Gets the key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public string Value { get; }
}
