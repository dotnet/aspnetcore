// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Localization;

/// <summary>
/// A delegate that resolves a localized display name for a property or parameter.
/// </summary>
/// <param name="context">
/// The <see cref="DisplayNameProviderContext"/> describing the member name, declaring type,
/// and services available for resolving the display name.
/// </param>
/// <returns>
/// A localized display name string, or <see langword="null"/> to fall back to the value of
/// <see cref="DisplayAttribute.Name"/> or the CLR member name.
/// </returns>
public delegate string? DisplayNameProvider(in DisplayNameProviderContext context);
