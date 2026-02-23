// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// A delegate that resolves a localized or customized error message for a <see cref="ValidationAttribute"/>.
/// </summary>
/// <param name="context">
/// The <see cref="ErrorMessageProviderContext"/> describing the attribute, member, and services
/// available for resolving the error message.
/// </param>
/// <returns>
/// A fully formatted error message string to use in place of the attribute's default,
/// or <see langword="null"/> to fall back to the attribute's default error message.
/// </returns>
public delegate string? ErrorMessageProvider(in ErrorMessageProviderContext context);
