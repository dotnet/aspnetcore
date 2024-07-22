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
    // Represents primitive types that should never be represented as
    // a schema reference and always inlined.
    internal static readonly List<Type> PrimitiveTypes =
    [
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(byte[]),
        typeof(string),
        typeof(int),
        typeof(uint),
        typeof(nint),
        typeof(nuint),
        typeof(Int128),
        typeof(UInt128),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(Half),
        typeof(ulong),
        typeof(short),
        typeof(ushort),
        typeof(char),
        typeof(object),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeOnly),
        typeof(DateOnly),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri),
        typeof(Version)
    ];
}
