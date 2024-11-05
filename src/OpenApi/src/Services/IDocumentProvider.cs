// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi;

namespace Microsoft.Extensions.ApiDescriptions;

/// <summary>
/// Represents a provider for OpenAPI documents to support build-time generation.
/// </summary>
/// <remarks>
/// The Microsoft.Extensions.ApiDescription.Server package and associated configuration
/// execute the `dotnet getdocument` command at build-time to support build-time
/// generation of documents. The `getdocument` tool launches the entry point assembly
/// and queries it for a service that implements the `IDocumentProvider` interface. For
/// historical reasons, the `IDocumentProvider` interface isn't exposed publicly from
/// the framework and the `getdocument` tool instead queries for it using the type name.
/// That means the `IDocumentProvider` interface must be declared under the namespace
/// that it expects. For more information, see https://github.com/dotnet/aspnetcore/blob/82c9b34d7206ba56ea1d641843e1f2fe6d2a0b1c/src/Tools/GetDocumentInsider/src/Commands/GetDocumentCommandWorker.cs#L25.
/// </remarks>
internal interface IDocumentProvider
{
    IEnumerable<string> GetDocumentNames();
    Task GenerateAsync(string documentName, TextWriter writer);
    Task GenerateAsync(string documentName, TextWriter writer, OpenApiSpecVersion openApiSpecVersion);
}
