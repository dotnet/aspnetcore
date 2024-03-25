// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal class OpenApiDocumentService(IHostEnvironment hostEnvironment)
{
    private readonly string _defaultOpenApiVersion = "1.0.0";

    public Task<OpenApiDocument> GetOpenApiDocumentAsync()
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = hostEnvironment.ApplicationName,
                Version = _defaultOpenApiVersion
            }
        };
        return Task.FromResult(document);
    }
}
