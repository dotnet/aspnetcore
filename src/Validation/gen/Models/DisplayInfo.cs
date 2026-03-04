// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Holds the resolved display name metadata for a symbol.
/// Created by <see cref="ISymbolExtensions.GetDisplayInfo"/>.
/// </summary>
/// <param name="Name">
/// The display name string. When <paramref name="ResourceType"/> is <see langword="null"/> this is
/// used as a literal display name. When <paramref name="ResourceType"/> is set, this is the name of
/// the static property on the resource type that provides the localized value.
/// </param>
/// <param name="ResourceType">
/// The resource type that contains a static property named <paramref name="Name"/>,
/// or <see langword="null"/> when the name is a plain string literal.
/// </param>
internal sealed record class DisplayInfo(string Name, INamedTypeSymbol? ResourceType);
