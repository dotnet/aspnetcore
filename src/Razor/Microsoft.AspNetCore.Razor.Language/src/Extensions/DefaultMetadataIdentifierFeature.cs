// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
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
            if (!identifier.StartsWith("/"))
            {
                identifier = "/" + identifier;
            }
            
            return identifier;
        }
    }
}
