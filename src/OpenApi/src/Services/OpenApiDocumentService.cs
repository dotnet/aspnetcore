// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiDocumentService(IHostEnvironment hostEnvironment)
{
    public Task<OpenApiDocument> GetOpenApiDocumentAsync()
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = hostEnvironment.ApplicationName,
                Version = OpenApiConstants.DefaultOpenApiVersion
            }
        };
        return Task.FromResult(document);
    }
}
