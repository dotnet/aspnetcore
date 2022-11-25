// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.App.Analyzers.Infrastructure;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

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

    public static readonly WellKnownType[] NumericTypes = new[]
    {
        WellKnownType.System_SByte,
        WellKnownType.System_Int16,
        WellKnownType.System_Int32,
        WellKnownType.System_Int64,
        WellKnownType.System_Byte,
        WellKnownType.System_UInt16,
        WellKnownType.System_UInt32,
        WellKnownType.System_UInt64,
        WellKnownType.System_Single,
        WellKnownType.System_Double,
        WellKnownType.System_Half,
        WellKnownType.System_Decimal,
        WellKnownType.System_IntPtr,
        WellKnownType.System_Numerics_BigInteger
    };

    public static readonly WellKnownType[] TemporalTypes = new[]
    {
        WellKnownType.System_DateTime,
        WellKnownType.System_DateTimeOffset,
        WellKnownType.System_DateOnly,
        WellKnownType.System_TimeOnly
    };
}
