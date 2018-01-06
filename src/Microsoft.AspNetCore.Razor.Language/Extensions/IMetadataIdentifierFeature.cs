// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal interface IMetadataIdentifierFeature : IRazorEngineFeature
    {
        string GetIdentifier(RazorCodeDocument codeDocument, RazorSourceDocument sourceDocument);
    }
}
