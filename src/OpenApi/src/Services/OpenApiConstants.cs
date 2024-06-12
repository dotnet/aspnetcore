// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiConstants
{
    internal const string DefaultDocumentName = "v1";
    internal const string DefaultOpenApiVersion = "1.0.0";
    internal const string DefaultOpenApiRoute = "/openapi/{documentName}.json";
    internal const string DescriptionId = "x-aspnetcore-id";
    internal const string SchemaId = "x-schema-id";
    internal const string DefaultOpenApiResponseKey = "default";
    // Since there's a finite set of operation types that can be included in a given
    // OpenApiPaths, we can pre-allocate an array of these types and use a direct
    // lookup on the OpenApiPaths dictionary to avoid allocating an enumerator
    // over the KeyValuePairs in OpenApiPaths.
    internal static readonly OperationType[] OperationTypes = [
        OperationType.Get,
        OperationType.Post,
        OperationType.Put,
        OperationType.Delete,
        OperationType.Options,
        OperationType.Head,
        OperationType.Patch,
        OperationType.Trace
    ];
}
