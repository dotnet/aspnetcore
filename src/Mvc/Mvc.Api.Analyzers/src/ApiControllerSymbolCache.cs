// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

internal readonly struct ApiControllerSymbolCache
{
    public static bool TryCreate(Compilation compilation, out ApiControllerSymbolCache symbolCache)
    {
        symbolCache = default;

        if (!TryGetType(ApiSymbolNames.ApiConventionMethodAttribute, out var apiConventionMethodAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ApiConventionNameMatchAttribute, out var apiConventionNameMatchAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ApiConventionTypeAttribute, out var apiConventionTypeAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ApiConventionTypeMatchAttribute, out var apiConventionTypeMatchAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ControllerAttribute, out var controllerAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.DefaultStatusCodeAttribute, out var defaultStatusCodeAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.IActionResult, out var iActionResult))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.IApiBehaviorMetadata, out var iApiBehaviorMetadata))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ModelStateDictionary, out var modelStateDictionary))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.NonActionAttribute, out var nonActionAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.NonControllerAttribute, out var nonControllerAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ProblemDetails, out var problemDetails))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ProducesDefaultResponseTypeAttribute, out var producesDefaultResponseTypeAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ProducesErrorResponseTypeAttribute, out var producesErrorResponseTypeAttribute))
        {
            return false;
        }

        if (!TryGetType(ApiSymbolNames.ProducesResponseTypeAttribute, out var producesResponseTypeAttribute))
        {
            return false;
        }

        var statusCodeActionResult = compilation.GetTypeByMetadataName(ApiSymbolNames.IStatusCodeActionResult);
        var statusCodeActionResultStatusProperty = (IPropertySymbol?)statusCodeActionResult?.GetMembers("StatusCode")[0];
        if (statusCodeActionResultStatusProperty == null)
        {
            return false;
        }

        var disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
        var members = disposable?.GetMembers(nameof(IDisposable.Dispose));
        var iDisposableDispose = (IMethodSymbol?)members?[0];
        if (iDisposableDispose == null)
        {
            return false;
        }

        symbolCache = new ApiControllerSymbolCache(
            apiConventionMethodAttribute,
            apiConventionNameMatchAttribute,
            apiConventionTypeAttribute,
            apiConventionTypeMatchAttribute,
            controllerAttribute,
            defaultStatusCodeAttribute,
            iActionResult,
            iApiBehaviorMetadata,
            iDisposableDispose,
            statusCodeActionResultStatusProperty,
            modelStateDictionary,
            nonActionAttribute,
            nonControllerAttribute,
            problemDetails,
            producesDefaultResponseTypeAttribute,
            producesResponseTypeAttribute,
            producesErrorResponseTypeAttribute);

        return true;

        bool TryGetType(string typeName, out INamedTypeSymbol typeSymbol)
        {
            typeSymbol = compilation.GetTypeByMetadataName(typeName);
            return typeSymbol != null && typeSymbol.TypeKind != TypeKind.Error;
        }
    }

    private ApiControllerSymbolCache(
        INamedTypeSymbol apiConventionMethodAttribute,
        INamedTypeSymbol apiConventionNameMatchAttribute,
        INamedTypeSymbol apiConventionTypeAttribute,
        INamedTypeSymbol apiConventionTypeMatchAttribute,
        INamedTypeSymbol controllerAttribute,
        INamedTypeSymbol defaultStatusCodeAttribute,
        INamedTypeSymbol actionResult,
        INamedTypeSymbol apiBehaviorMetadata,
        IMethodSymbol disposableDispose,
        IPropertySymbol statusCodeActionResultStatusProperty,
        ITypeSymbol modelStateDictionary,
        INamedTypeSymbol nonActionAttribute,
        INamedTypeSymbol nonControllerAttribute,
        INamedTypeSymbol problemDetails,
        INamedTypeSymbol producesDefaultResponseTypeAttribute,
        INamedTypeSymbol producesResponseTypeAttribute,
        INamedTypeSymbol producesErrorResponseTypeAttribute)
    {
        ApiConventionMethodAttribute = apiConventionMethodAttribute;
        ApiConventionNameMatchAttribute = apiConventionNameMatchAttribute;
        ApiConventionTypeAttribute = apiConventionTypeAttribute;
        ApiConventionTypeMatchAttribute = apiConventionTypeMatchAttribute;
        ControllerAttribute = controllerAttribute;
        DefaultStatusCodeAttribute = defaultStatusCodeAttribute;
        IActionResult = actionResult;
        IApiBehaviorMetadata = apiBehaviorMetadata;
        IDisposableDispose = disposableDispose;
        StatusCodeActionResultStatusProperty = statusCodeActionResultStatusProperty;
        ModelStateDictionary = modelStateDictionary;
        NonActionAttribute = nonActionAttribute;
        NonControllerAttribute = nonControllerAttribute;
        ProblemDetails = problemDetails;
        ProducesDefaultResponseTypeAttribute = producesDefaultResponseTypeAttribute;
        ProducesResponseTypeAttribute = producesResponseTypeAttribute;
        ProducesErrorResponseTypeAttribute = producesErrorResponseTypeAttribute;
    }

    public INamedTypeSymbol ApiConventionMethodAttribute { get; }

    public INamedTypeSymbol ApiConventionNameMatchAttribute { get; }

    public INamedTypeSymbol ApiConventionTypeAttribute { get; }

    public INamedTypeSymbol ApiConventionTypeMatchAttribute { get; }

    public INamedTypeSymbol ControllerAttribute { get; }

    public INamedTypeSymbol DefaultStatusCodeAttribute { get; }

    public INamedTypeSymbol IActionResult { get; }

    public INamedTypeSymbol IApiBehaviorMetadata { get; }

    public IMethodSymbol IDisposableDispose { get; }

    public IPropertySymbol StatusCodeActionResultStatusProperty { get; }

    public ITypeSymbol ModelStateDictionary { get; }

    public INamedTypeSymbol NonActionAttribute { get; }

    public INamedTypeSymbol NonControllerAttribute { get; }

    public INamedTypeSymbol ProblemDetails { get; }

    public INamedTypeSymbol ProducesDefaultResponseTypeAttribute { get; }

    public INamedTypeSymbol ProducesResponseTypeAttribute { get; }

    public INamedTypeSymbol ProducesErrorResponseTypeAttribute { get; }
}
