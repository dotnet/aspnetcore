// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal sealed record class RequiredSymbols(
    INamedTypeSymbol DisplayAttribute,
    INamedTypeSymbol ValidationAttribute,
    INamedTypeSymbol IEnumerable,
    INamedTypeSymbol IValidatableObject,
    INamedTypeSymbol JsonDerivedTypeAttribute,
    INamedTypeSymbol RequiredAttribute,
    INamedTypeSymbol CustomValidationAttribute,
    INamedTypeSymbol HttpContext,
    INamedTypeSymbol HttpRequest,
    INamedTypeSymbol HttpResponse,
    INamedTypeSymbol CancellationToken,
    INamedTypeSymbol IFormCollection,
    INamedTypeSymbol IFormFileCollection,
    INamedTypeSymbol IFormFile,
    INamedTypeSymbol Stream,
    INamedTypeSymbol PipeReader
);
