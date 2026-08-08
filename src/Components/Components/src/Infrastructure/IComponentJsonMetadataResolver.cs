// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides the application's JSON contracts, used wherever the framework serializes an application
/// type and the reflection-based contract resolver is unavailable.
/// </summary>
internal interface IComponentJsonMetadataResolver
{
    /// <summary>
    /// Gets the application's resolver, or <see langword="null"/> when it supplied none.
    /// </summary>
    IJsonTypeInfoResolver? JsonTypeInfoResolver { get; }
}
