// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

internal sealed class WellKnownTypes
{
    /// <summary>
    /// Cache so that we can reuse the same <see cref="WellKnownTypes"/> when analyzing a particular compilation model.
    /// </summary>
    private static readonly ConditionalWeakTable<Compilation, WellKnownTypes?> _compilationToTypes = new();

    public static bool TryGetOrCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = _compilationToTypes.GetValue(compilation, static c =>
        {
            TryCreate(c, out var wellKnownTypes);
            return wellKnownTypes;
        });

        // The cache could return null if well known types couldn't be resolved.
        return wellKnownTypes != null;
    }

    private static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out WellKnownTypes? wellKnownTypes)
    {
        wellKnownTypes = default;

        const string IFromBodyMetadata = "Microsoft.AspNetCore.Http.Metadata.IFromBodyMetadata";
        if (compilation.GetTypeByMetadataName(IFromBodyMetadata) is not { } iFromBodyMetadata)
        {
            return false;
        }

        const string IFromFormMetadata = "Microsoft.AspNetCore.Http.Metadata.IFromFormMetadata";
        if (compilation.GetTypeByMetadataName(IFromFormMetadata) is not { } iFromFormMetadata)
        {
            return false;
        }

        const string IFromHeaderMetadata = "Microsoft.AspNetCore.Http.Metadata.IFromHeaderMetadata";
        if (compilation.GetTypeByMetadataName(IFromHeaderMetadata) is not { } iFromHeaderMetadata)
        {
            return false;
        }

        const string IFromQueryMetadata = "Microsoft.AspNetCore.Http.Metadata.IFromQueryMetadata";
        if (compilation.GetTypeByMetadataName(IFromQueryMetadata) is not { } iFromQueryMetadata)
        {
            return false;
        }

        const string IFromServiceMetadata = "Microsoft.AspNetCore.Http.Metadata.IFromServiceMetadata";
        if (compilation.GetTypeByMetadataName(IFromServiceMetadata) is not { } iFromServiceMetadata)
        {
            return false;
        }

        const string IEndpointRouteBuilder = "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder";
        if (compilation.GetTypeByMetadataName(IEndpointRouteBuilder) is not { } iEndpointRouteBuilder)
        {
            return false;
        }

        const string ControllerAttribute = "Microsoft.AspNetCore.Mvc.ControllerAttribute";
        if (compilation.GetTypeByMetadataName(ControllerAttribute) is not { } controllerAttribute)
        {
            return false;
        }

        const string NonControllerAttribute = "Microsoft.AspNetCore.Mvc.NonControllerAttribute";
        if (compilation.GetTypeByMetadataName(NonControllerAttribute) is not { } nonControllerAttribute)
        {
            return false;
        }

        const string NonActionAttribute = "Microsoft.AspNetCore.Mvc.NonActionAttribute";
        if (compilation.GetTypeByMetadataName(NonActionAttribute) is not { } nonActionAttribute)
        {
            return false;
        }

        const string AsParametersAttribute = "Microsoft.AspNetCore.Http.AsParametersAttribute";
        if (compilation.GetTypeByMetadataName(AsParametersAttribute) is not { } asParametersAttribute)
        {
            return false;
        }

        const string CancellationToken = "System.Threading.CancellationToken";
        if (compilation.GetTypeByMetadataName(CancellationToken) is not { } cancellationToken)
        {
            return false;
        }

        const string HttpContext = "Microsoft.AspNetCore.Http.HttpContext";
        if (compilation.GetTypeByMetadataName(HttpContext) is not { } httpContext)
        {
            return false;
        }

        const string HttpRequest = "Microsoft.AspNetCore.Http.HttpRequest";
        if (compilation.GetTypeByMetadataName(HttpRequest) is not { } httpRequest)
        {
            return false;
        }

        const string HttpResponse = "Microsoft.AspNetCore.Http.HttpResponse";
        if (compilation.GetTypeByMetadataName(HttpResponse) is not { } httpResponse)
        {
            return false;
        }

        const string ClaimsPrincipal = "System.Security.Claims.ClaimsPrincipal";
        if (compilation.GetTypeByMetadataName(ClaimsPrincipal) is not { } claimsPrincipal)
        {
            return false;
        }

        const string IFormFileCollection = "Microsoft.AspNetCore.Http.IFormFileCollection";
        if (compilation.GetTypeByMetadataName(IFormFileCollection) is not { } iFormFileCollection)
        {
            return false;
        }

        const string IFormFile = "Microsoft.AspNetCore.Http.IFormFile";
        if (compilation.GetTypeByMetadataName(IFormFile) is not { } iFormFile)
        {
            return false;
        }

        const string Stream = "System.IO.Stream";
        if (compilation.GetTypeByMetadataName(Stream) is not { } stream)
        {
            return false;
        }

        const string PipeReader = "System.IO.Pipelines.PipeReader";
        if (compilation.GetTypeByMetadataName(PipeReader) is not { } pipeReader)
        {
            return false;
        }

        const string IFormatProvider = "System.IFormatProvider";
        if (compilation.GetTypeByMetadataName(IFormatProvider) is not { } iFormatProvider)
        {
            return false;
        }

        const string Uri = "System.Uri";
        if (compilation.GetTypeByMetadataName(Uri) is not { } uri)
        {
            return false;
        }

        wellKnownTypes = new()
        {
            IFromBodyMetadata = iFromBodyMetadata,
            IFromFormMetadata = iFromFormMetadata,
            IFromHeaderMetadata = iFromHeaderMetadata,
            IFromQueryMetadata = iFromQueryMetadata,
            IFromServiceMetadata = iFromServiceMetadata,
            IEndpointRouteBuilder = iEndpointRouteBuilder,
            ControllerAttribute = controllerAttribute,
            NonControllerAttribute = nonControllerAttribute,
            NonActionAttribute = nonActionAttribute,
            AsParametersAttribute = asParametersAttribute,
            CancellationToken = cancellationToken,
            HttpContext = httpContext,
            HttpRequest = httpRequest,
            HttpResponse = httpResponse,
            ClaimsPrincipal = claimsPrincipal,
            IFormFileCollection = iFormFileCollection,
            IFormFile = iFormFile,
            Stream = stream,
            PipeReader = pipeReader,
            IFormatProvider = iFormatProvider,
            Uri = uri,
        };

        return true;
    }

    public INamedTypeSymbol IFromBodyMetadata { get; private init; }
    public INamedTypeSymbol IFromFormMetadata { get; private init; }
    public INamedTypeSymbol IFromHeaderMetadata { get; private init; }
    public INamedTypeSymbol IFromQueryMetadata { get; private init; }
    public INamedTypeSymbol IFromServiceMetadata { get; private init; }
    public INamedTypeSymbol IEndpointRouteBuilder { get; private init; }
    public INamedTypeSymbol ControllerAttribute { get; private init; }
    public INamedTypeSymbol NonControllerAttribute { get; private init; }
    public INamedTypeSymbol NonActionAttribute { get; private init; }
    public INamedTypeSymbol AsParametersAttribute { get; private init; }
    public INamedTypeSymbol CancellationToken { get; private init; }
    public INamedTypeSymbol HttpContext { get; private init; }
    public INamedTypeSymbol HttpRequest { get; private init; }
    public INamedTypeSymbol HttpResponse { get; private init; }
    public INamedTypeSymbol ClaimsPrincipal { get; private init; }
    public INamedTypeSymbol IFormFileCollection { get; private init; }
    public INamedTypeSymbol Stream { get; private init; }
    public INamedTypeSymbol PipeReader { get; private init; }
    public INamedTypeSymbol IFormFile { get; private init; }
    public INamedTypeSymbol IFormatProvider { get; private init; }
    public INamedTypeSymbol Uri { get; private init; }

    private INamedTypeSymbol[]? _parameterSpecialTypes;
    public INamedTypeSymbol[] ParameterSpecialTypes
    {
        get
        {
            _parameterSpecialTypes ??= new[]
            {
                CancellationToken,
                HttpContext,
                HttpRequest,
                HttpResponse,
                ClaimsPrincipal,
                IFormFileCollection,
                IFormFile,
                Stream,
                PipeReader
            };
            return _parameterSpecialTypes;
        }
    }

    private INamedTypeSymbol[]? _nonRouteMetadataTypes;
    public INamedTypeSymbol[] NonRouteMetadataTypes
    {
        get
        {
            _nonRouteMetadataTypes ??= new[]
            {
                IFromBodyMetadata,
                IFromFormMetadata,
                IFromHeaderMetadata,
                IFromQueryMetadata,
                IFromServiceMetadata
            };
            return _nonRouteMetadataTypes;
        }
    }
}
