// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

// The default scheme for identifiers matches MVC's view engine paths:
// 1. Normalize backslash to forward-slash
// 2. Always include leading slash
// 3. Always include file name and extensions
internal class DefaultMetadataIdentifierFeature : RazorEngineFeatureBase, IMetadataIdentifierFeature
{
    public string GetIdentifier(RazorCodeDocument codeDocument, RazorSourceDocument sourceDocument)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        if (sourceDocument == null)
        {
            throw new ArgumentNullException(nameof(sourceDocument));
        }

        if (string.IsNullOrEmpty(sourceDocument.RelativePath))
        {
            return null;
        }

        var identifier = sourceDocument.RelativePath;
        identifier = identifier.Replace("\\", "/");
        if (!identifier.StartsWith("/", StringComparison.Ordinal))
        {
            identifier = "/" + identifier;
        }

        return identifier;
    }
}
