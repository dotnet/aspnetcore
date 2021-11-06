// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

/// <summary>
/// A <see cref="IRazorEngineFeature"/> that can calculate the identifier for a <see cref="RazorSourceDocument"/>.
/// </summary>
public interface IMetadataIdentifierFeature : IRazorEngineFeature
{
    /// <summary>
    /// Gets the identifier for a <see cref="RazorSourceDocument"/>.
    /// </summary>
    /// <param name="codeDocument">The <see cref="RazorCodeDocument"/>.</param>
    /// <param name="sourceDocument">The <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The identifier.</returns>
    string GetIdentifier(RazorCodeDocument codeDocument, RazorSourceDocument sourceDocument);
}
