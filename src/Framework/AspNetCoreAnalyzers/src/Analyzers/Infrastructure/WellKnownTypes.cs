// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal enum WellKnownType
{
    Microsoft_AspNetCore_Components_Rendering_RenderTreeBuilder,
    Microsoft_AspNetCore_Http_IHeaderDictionary,
    Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata,
    Microsoft_AspNetCore_Http_Metadata_IFromFormMetadata,
    Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata,
    Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata,
    Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata,
    Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata,
    Microsoft_AspNetCore_Http_HeaderDictionaryExtensions,
    Microsoft_AspNetCore_Routing_IEndpointRouteBuilder,
    Microsoft_AspNetCore_Mvc_ControllerAttribute,
    Microsoft_AspNetCore_Mvc_NonControllerAttribute,
    Microsoft_AspNetCore_Mvc_NonActionAttribute,
    Microsoft_AspNetCore_Http_AsParametersAttribute,
    System_Threading_CancellationToken,
    Microsoft_AspNetCore_Http_HttpContext,
    Microsoft_AspNetCore_Http_HttpRequest,
    Microsoft_AspNetCore_Http_HttpResponse,
    System_Security_Claims_ClaimsPrincipal,
    Microsoft_AspNetCore_Http_IFormFileCollection,
    Microsoft_AspNetCore_Http_IFormFile,
    System_IO_Stream,
    System_IO_Pipelines_PipeReader,
    System_IFormatProvider,
    System_Uri,
    Microsoft_AspNetCore_Builder_ConfigureHostBuilder,
    Microsoft_AspNetCore_Builder_ConfigureWebHostBuilder,
    Microsoft_Extensions_Hosting_GenericHostWebHostBuilderExtensions,
    Microsoft_AspNetCore_Hosting_WebHostBuilderExtensions,
    Microsoft_AspNetCore_Hosting_HostingAbstractionsWebHostBuilderExtensions,
    Microsoft_Extensions_Hosting_HostingHostBuilderExtensions,
    Microsoft_AspNetCore_Builder_EndpointRoutingApplicationBuilderExtensions,
    Microsoft_AspNetCore_Builder_WebApplication,
    Microsoft_AspNetCore_Builder_EndpointRouteBuilderExtensions,
    System_Delegate,
    Microsoft_AspNetCore_Mvc_ModelBinding_IBinderTypeProviderMetadata,
    Microsoft_AspNetCore_Mvc_BindAttribute,
    Microsoft_AspNetCore_Http_IResult,
    Microsoft_AspNetCore_Mvc_IActionResult,
    Microsoft_AspNetCore_Mvc_Infrastructure_IConvertToActionResult,
    Microsoft_AspNetCore_Http_RequestDelegate,
    System_Threading_Tasks_Task_T,
    System_Threading_Tasks_ValueTask_T,
    System_Reflection_ParameterInfo,
    Microsoft_AspNetCore_Http_IBindableFromHttpContext_T,
    System_IParsable_T,
    Microsoft_AspNetCore_Builder_AuthorizationEndpointConventionBuilderExtensions,
    Microsoft_AspNetCore_Http_OpenApiRouteHandlerBuilderExtensions,
    Microsoft_AspNetCore_Builder_CorsEndpointConventionBuilderExtensions,
    Microsoft_Extensions_DependencyInjection_OutputCacheConventionBuilderExtensions,
    Microsoft_AspNetCore_Builder_RateLimiterEndpointConventionBuilderExtensions,
    Microsoft_AspNetCore_Builder_RoutingEndpointConventionBuilderExtensions,
    Microsoft_AspNetCore_Mvc_RouteAttribute,
    Microsoft_AspNetCore_Mvc_HttpDeleteAttribute,
    Microsoft_AspNetCore_Mvc_HttpGetAttribute,
    Microsoft_AspNetCore_Mvc_HttpHeadAttribute,
    Microsoft_AspNetCore_Mvc_HttpOptionsAttribute,
    Microsoft_AspNetCore_Mvc_HttpPatchAttribute,
    Microsoft_AspNetCore_Mvc_HttpPostAttribute,
    Microsoft_AspNetCore_Mvc_HttpPutAttribute,
    Microsoft_AspNetCore_Http_EndpointDescriptionAttribute,
    Microsoft_AspNetCore_Http_EndpointSummaryAttribute,
    Microsoft_AspNetCore_Http_TagsAttribute,
    Microsoft_AspNetCore_Routing_EndpointGroupNameAttribute,
    Microsoft_AspNetCore_Routing_EndpointNameAttribute,
    Microsoft_AspNetCore_Routing_ExcludeFromDescriptionAttribute,
    Microsoft_AspNetCore_Cors_DisableCorsAttribute,
    Microsoft_AspNetCore_Cors_EnableCorsAttribute,
    Microsoft_AspNetCore_OutputCaching_OutputCacheAttribute,
    Microsoft_AspNetCore_RateLimiting_DisableRateLimitingAttribute,
    Microsoft_AspNetCore_RateLimiting_EnableRateLimitingAttribute,
    Microsoft_AspNetCore_Mvc_ActionNameAttribute,
    Microsoft_AspNetCore_Mvc_DisableRequestSizeLimitAttribute,
    Microsoft_AspNetCore_Mvc_FormatFilterAttribute,
    Microsoft_AspNetCore_Mvc_ProducesAttribute,
    Microsoft_AspNetCore_Mvc_ProducesDefaultResponseTypeAttribute,
    Microsoft_AspNetCore_Mvc_ProducesErrorResponseTypeAttribute,
    Microsoft_AspNetCore_Mvc_ProducesResponseTypeAttribute,
    Microsoft_AspNetCore_Mvc_RequestFormLimitsAttribute,
    Microsoft_AspNetCore_Mvc_RequestSizeLimitAttribute,
    Microsoft_AspNetCore_Mvc_RequireHttpsAttribute,
    Microsoft_AspNetCore_Mvc_ResponseCacheAttribute,
    Microsoft_AspNetCore_Mvc_ServiceFilterAttribute,
    Microsoft_AspNetCore_Mvc_TypeFilterAttribute,
    Microsoft_AspNetCore_Mvc_ApiExplorer_ApiConventionNameMatchAttribute,
    Microsoft_AspNetCore_Mvc_Filters_ResultFilterAttribute,
    Microsoft_AspNetCore_Mvc_Infrastructure_DefaultStatusCodeAttribute,
    Microsoft_AspNetCore_Mvc_AutoValidateAntiforgeryTokenAttribute,
    Microsoft_AspNetCore_Mvc_IgnoreAntiforgeryTokenAttribute,
    Microsoft_AspNetCore_Mvc_ViewFeatures_SaveTempDataAttribute,
    Microsoft_AspNetCore_Mvc_SkipStatusCodePagesAttribute,
    Microsoft_AspNetCore_Mvc_ValidateAntiForgeryTokenAttribute,
    Microsoft_AspNetCore_Authorization_AllowAnonymousAttribute,
    Microsoft_AspNetCore_Authorization_AuthorizeAttribute
}

internal sealed class WellKnownTypes
{
    private static readonly BoundedCacheWithFactory<Compilation, WellKnownTypes> LazyWellKnownTypesCache = new();
    private static readonly string[] WellKnownTypeNames = new[]
    {
        "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder",
        "Microsoft.AspNetCore.Http.IHeaderDictionary",
        "Microsoft.AspNetCore.Http.Metadata.IFromBodyMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromFormMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromHeaderMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromQueryMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromServiceMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromRouteMetadata",
        "Microsoft.AspNetCore.Http.HeaderDictionaryExtensions",
        "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder",
        "Microsoft.AspNetCore.Mvc.ControllerAttribute",
        "Microsoft.AspNetCore.Mvc.NonControllerAttribute",
        "Microsoft.AspNetCore.Mvc.NonActionAttribute",
        "Microsoft.AspNetCore.Http.AsParametersAttribute",
        "System.Threading.CancellationToken",
        "Microsoft.AspNetCore.Http.HttpContext",
        "Microsoft.AspNetCore.Http.HttpRequest",
        "Microsoft.AspNetCore.Http.HttpResponse",
        "System.Security.Claims.ClaimsPrincipal",
        "Microsoft.AspNetCore.Http.IFormFileCollection",
        "Microsoft.AspNetCore.Http.IFormFile",
        "System.IO.Stream",
        "System.IO.Pipelines.PipeReader",
        "System.IFormatProvider",
        "System.Uri",
        "Microsoft.AspNetCore.Builder.ConfigureHostBuilder",
        "Microsoft.AspNetCore.Builder.ConfigureWebHostBuilder",
        "Microsoft.Extensions.Hosting.GenericHostWebHostBuilderExtensions",
        "Microsoft.AspNetCore.Hosting.WebHostBuilderExtensions",
        "Microsoft.AspNetCore.Hosting.HostingAbstractionsWebHostBuilderExtensions",
        "Microsoft.Extensions.Hosting.HostingHostBuilderExtensions",
        "Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions",
        "Microsoft.AspNetCore.Builder.WebApplication",
        "Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions",
        "System.Delegate",
        "Microsoft.AspNetCore.Mvc.ModelBinding.IBinderTypeProviderMetadata",
        "Microsoft.AspNetCore.Mvc.BindAttribute",
        "Microsoft.AspNetCore.Http.IResult",
        "Microsoft.AspNetCore.Mvc.IActionResult",
        "Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult",
        "Microsoft.AspNetCore.Http.RequestDelegate",
        "System.Threading.Tasks.Task`1",
        "System.Threading.Tasks.ValueTask`1",
        "System.Reflection.ParameterInfo",
        "Microsoft.AspNetCore.Http.IBindableFromHttpContext`1",
        "System.IParsable`1",
        "Microsoft.AspNetCore.Builder.AuthorizationEndpointConventionBuilderExtensions",
        "Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions",
        "Microsoft.AspNetCore.Builder.CorsEndpointConventionBuilderExtensions",
        "Microsoft.Extensions.DependencyInjection.OutputCacheConventionBuilderExtensions",
        "Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions",
        "Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions",
        "Microsoft.AspNetCore.Mvc.RouteAttribute",
        "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute",
        "Microsoft.AspNetCore.Mvc.HttpGetAttribute",
        "Microsoft.AspNetCore.Mvc.HttpHeadAttribute",
        "Microsoft.AspNetCore.Mvc.HttpOptionsAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPatchAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPostAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPutAttribute",
        "Microsoft.AspNetCore.Http.EndpointDescriptionAttribute",
        "Microsoft.AspNetCore.Http.EndpointSummaryAttribute",
        "Microsoft.AspNetCore.Http.TagsAttribute",
        "Microsoft.AspNetCore.Routing.EndpointGroupNameAttribute",
        "Microsoft.AspNetCore.Routing.EndpointNameAttribute",
        "Microsoft.AspNetCore.Routing.ExcludeFromDescriptionAttribute",
        "Microsoft.AspNetCore.Cors.DisableCorsAttribute",
        "Microsoft.AspNetCore.Cors.EnableCorsAttribute",
        "Microsoft.AspNetCore.OutputCaching.OutputCacheAttribute",
        "Microsoft.AspNetCore.RateLimiting.DisableRateLimitingAttribute",
        "Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute",
        "Microsoft.AspNetCore.Mvc.ActionNameAttribute",
        "Microsoft.AspNetCore.Mvc.DisableRequestSizeLimitAttribute",
        "Microsoft.AspNetCore.Mvc.FormatFilterAttribute",
        "Microsoft.AspNetCore.Mvc.ProducesAttribute",
        "Microsoft.AspNetCore.Mvc.ProducesDefaultResponseTypeAttribute",
        "Microsoft.AspNetCore.Mvc.ProducesErrorResponseTypeAttribute",
        "Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute",
        "Microsoft.AspNetCore.Mvc.RequestFormLimitsAttribute",
        "Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute",
        "Microsoft.AspNetCore.Mvc.RequireHttpsAttribute",
        "Microsoft.AspNetCore.Mvc.ResponseCacheAttribute",
        "Microsoft.AspNetCore.Mvc.ServiceFilterAttribute",
        "Microsoft.AspNetCore.Mvc.TypeFilterAttribute",
        "Microsoft.AspNetCore.Mvc.ApiExplorer.ApiConventionNameMatchAttribute",
        "Microsoft.AspNetCore.Mvc.Filters.ResultFilterAttribute",
        "Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCodeAttribute",
        "Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute",
        "Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute",
        "Microsoft.AspNetCore.Mvc.ViewFeatures.SaveTempDataAttribute",
        "Microsoft.AspNetCore.Mvc.SkipStatusCodePagesAttribute",
        "Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute",
        "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute",
        "Microsoft.AspNetCore.Authorization.AuthorizeAttribute"
    };

    public static WellKnownTypes GetOrCreate(Compilation compilation) =>
        LazyWellKnownTypesCache.GetOrCreateValue(compilation, static c => new WellKnownTypes(c));

    private readonly INamedTypeSymbol?[] _lazyWellKnownTypes;
    private readonly Compilation _compilation;

    static WellKnownTypes()
    {
        AssertEnumAndTableInSync();
    }

    [Conditional("DEBUG")]
    private static void AssertEnumAndTableInSync()
    {
        for (var i = 0; i < WellKnownTypeNames.Length; i++)
        {
            var name = WellKnownTypeNames[i];
            var typeId = (WellKnownType)i;

            var typeIdName = typeId.ToString().Replace("__", "+").Replace('_', '.');

            var separator = name.IndexOf('`');
            if (separator >= 0)
            {
                // Ignore type parameter qualifier for generic types.
                name = name.Substring(0, separator);
                typeIdName = typeIdName.Substring(0, separator);
            }

            Debug.Assert(name == typeIdName, $"Enum name ({typeIdName}) and type name ({name}) must match at {i}");
        }
    }

    private WellKnownTypes(Compilation compilation)
    {
        _lazyWellKnownTypes = new INamedTypeSymbol?[WellKnownTypeNames.Length];
        _compilation = compilation;
    }

    public INamedTypeSymbol Get(SpecialType type)
    {
        return _compilation.GetSpecialType(type);
    }

    public INamedTypeSymbol Get(WellKnownType type)
    {
        var index = (int)type;
        var symbol = _lazyWellKnownTypes[index];
        if (symbol is not null)
        {
            return symbol;
        }

        // Symbol hasn't been added to the cache yet.
        // Resolve symbol from name, cache, and return.
        return GetAndCache(index);
    }

    private INamedTypeSymbol GetAndCache(int index)
    {
        var result = _compilation.GetTypeByMetadataName(WellKnownTypeNames[index]);
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to resolve well-known type '{WellKnownTypeNames[index]}'.");
        }
        Interlocked.CompareExchange(ref _lazyWellKnownTypes[index], result, null);

        // GetTypeByMetadataName should always return the same instance for a name.
        // To ensure we have a consistent value, for thread safety, return symbol set in the array.
        return _lazyWellKnownTypes[index]!;
    }

    public bool IsType(ITypeSymbol type, WellKnownType[] wellKnownTypes) => IsType(type, wellKnownTypes, out var _);

    public bool IsType(ITypeSymbol type, WellKnownType[] wellKnownTypes, [NotNullWhen(true)] out WellKnownType? match)
    {
        foreach (var wellKnownType in wellKnownTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(type, Get(wellKnownType)))
            {
                match = wellKnownType;
                return true;
            }
        }

        match = null;
        return false;
    }

    public bool Implements(ITypeSymbol type, WellKnownType[] interfaceWellKnownTypes)
    {
        foreach (var wellKnownType in interfaceWellKnownTypes)
        {
            if (type.Implements(Get(wellKnownType)))
            {
                return true;
            }
        }

        return false;
    }
}
