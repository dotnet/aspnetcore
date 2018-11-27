// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    [Export(typeof(DocumentServiceProviderFactory))]
    internal class DefaultDocumentServiceProviderFactory : DocumentServiceProviderFactory
    {
        public override IDocumentServiceProvider Create(DocumentSnapshot document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return new RazorDocumentServiceProvider(document);
        }

        public override IDocumentServiceProvider CreateEmpty()
        {
            return new RazorDocumentServiceProvider();
        }
    }
}
