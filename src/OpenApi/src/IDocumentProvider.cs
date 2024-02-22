// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ApiDescriptions;
internal interface IDocumentProvider
{
    IEnumerable<string> GetDocumentNames();
    Task GenerateAsync(string documentName, TextWriter writer);
}
