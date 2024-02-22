// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Writers;
using Microsoft.Extensions.ApiDescriptions;

internal class OpenApiDocumentProvider(OpenApiDocumentService documentService) : IDocumentProvider
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="documentName"></param>
    /// <param name="writer"></param>
    /// <returns></returns>
    public Task GenerateAsync(string documentName, TextWriter writer)
    {
        var document = documentService.Document;
        var jsonWriter = new OpenApiJsonWriter(writer);
        document.SerializeAsV3(jsonWriter);
        return Task.CompletedTask;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetDocumentNames()
    {
        return ["stub"];
    }
}
