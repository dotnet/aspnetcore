// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// Represents a method that returns an error message key based on the provided context.
/// </summary>
/// <param name="context">The context containing information about the validation attribute and member being validated.</param>
/// <returns>The error message key, or <see langword="null"/> if no key is selected.</returns>
public delegate string? ErrorMessageKeyProvider(in ErrorMessageProviderContext context);
