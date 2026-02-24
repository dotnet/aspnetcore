// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Provides context to the <see cref="ValidationOptions.DisplayNameProvider"/> delegate
/// for resolving localized display names for properties and parameters.
/// </summary>
public readonly struct DisplayNameProviderContext
{
    /// <summary>
    /// Gets the type that declares the member being validated.
    /// <see langword="null"/> for top-level parameter validation.
    /// </summary>
    public Type? DeclaringType { get; init; }

    /// <summary>
    /// Gets the display name from <see cref="DisplayAttribute.Name"/> to use as a localization lookup key.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the service provider for resolving localization services.
    /// </summary>
    public required IServiceProvider Services { get; init; }
}
