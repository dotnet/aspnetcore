// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal sealed record class DisplayInfo(string Name, INamedTypeSymbol? ResourceType);
