// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Writers;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents an <see cref="IOpenApiAny"/> instance that does not serialize itself to
/// the outgoing document.
///
/// The no-op implementation of the <see cref="Write(IOpenApiWriter, OpenApiSpecVersion)"/> method
/// prevents the value of these properties from being written to disk. When used in conjunction with
/// the logic to exempt these properties from serialization in <see cref="ScrubbingOpenApiJsonWriter"/>,
/// we achieve the desired result of not serializing these properties to the output document but retaining
/// them in the in-memory document.
/// </summary>
internal sealed class ScrubbedOpenApiAny(string? value) : IOpenApiAny
{
    public AnyType AnyType { get; } = AnyType.Primitive;

    public string? Value { get; } = value;

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        return;
    }
}
