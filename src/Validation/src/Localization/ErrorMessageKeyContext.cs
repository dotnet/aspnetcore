// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation.Localization;

public readonly struct ErrorMessageKeyContext
{
    public string MemberName { get; init; }

    public Type? ValidatorType { get; init; }

    /// <summary>
    /// Gets the type that declares the member being validated.
    /// <see langword="null"/> for top-level parameter validation.
    /// </summary>
    public Type? DeclaringType { get; init; }
}
