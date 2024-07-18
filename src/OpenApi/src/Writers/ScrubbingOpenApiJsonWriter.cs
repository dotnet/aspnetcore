// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Writers;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents a JSON writer that scrubs certain properties from the output,
/// specifically the schema ID and description ID that are used for schema resolution
/// and action descriptor resolution in the in-memory OpenAPI document.
///
/// In conjunction with <see cref="ScrubbedOpenApiAny" /> this allows us to work around
/// the lack of an in-memory property bag on the OpenAPI object model and allows us to
/// avoid having to scrub the properties in the OpenAPI document prior to serialization.
///
/// For more information, see  https://github.com/microsoft/OpenAPI.NET/issues/1719.
/// </summary>
internal sealed class ScrubbingOpenApiJsonWriter(TextWriter textWriter) : OpenApiJsonWriter(textWriter)
{
    public override void WritePropertyName(string name)
    {
        if (name == OpenApiConstants.SchemaId || name == OpenApiConstants.DescriptionId)
        {
            return;
        }

        base.WritePropertyName(name);
    }
}
