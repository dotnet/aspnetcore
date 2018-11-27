// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DOCUMENT_SERVICE_FACTORY

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IDocumentServiceProvider
    {
        /// <summary>
        /// Gets a document specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns null.
        /// </summary>
        TService GetService<TService>() where TService : class, IDocumentService;
    }
}

#endif