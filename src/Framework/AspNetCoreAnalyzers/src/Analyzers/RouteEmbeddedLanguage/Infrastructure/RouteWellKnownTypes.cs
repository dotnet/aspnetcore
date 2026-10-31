// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.App.Analyzers.Infrastructure;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

using WellKnownType = WellKnownTypeData.WellKnownType;

internal static class RouteWellKnownTypes
{
    // Cache well known type keys rather than symbol instances.
    // Well known type keys are constant while symbol instances will change between compilations.
    public static readonly WellKnownType[] ParameterSpecialTypes = new[]
    {
        WellKnownType.System_Threading_CancellationToken,
        WellKnownType.Microsoft_AspNetCore_Http_HttpContext,
        WellKnownType.Microsoft_AspNetCore_Http_HttpRequest,
        WellKnownType.Microsoft_AspNetCore_Http_HttpResponse,
        WellKnownType.System_Security_Claims_ClaimsPrincipal,
        WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection,
        WellKnownType.Microsoft_AspNetCore_Http_IFormFile,
        WellKnownType.System_IO_Stream,
        WellKnownType.System_IO_Pipelines_PipeReader,
    };

    public static readonly WellKnownType[] NonRouteMetadataTypes = new[]
    {
        WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata,
        WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromFormMetadata,
        WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata,
        WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata,
        WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata,
    };
}
