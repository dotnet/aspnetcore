// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Used to set the result of anti-forgery token validation.
/// </summary>
public interface IAntiforgeryValidationFeature
{
    /// <summary>
    /// Gets a value that determines if the anti-forgery token on the request is valid.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the <see cref="Exception"/> that occurred when validating the anti-forgery token.
    /// </summary>
    [MemberNotNullWhen(false, nameof(IsValid))]
    Exception? Error { get; }
}
