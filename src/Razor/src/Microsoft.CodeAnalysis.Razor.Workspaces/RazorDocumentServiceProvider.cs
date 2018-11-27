// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Host
{
    internal class RazorDocumentServiceProvider : IDocumentServiceProvider, IDocumentOperationService
    {
        private readonly DocumentSnapshot _document;
        private readonly object _lock;

        private RazorSpanMappingService _spanMappingService;
        private RazorDocumentExcerptService _excerptService;

        public RazorDocumentServiceProvider()
            : this(null)
        {
        }

        public RazorDocumentServiceProvider(DocumentSnapshot document)
        {
            _document = document;

            _lock = new object();
        }

        public bool CanApplyChange => false;

        public bool SupportDiagnostics => false;

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            if (typeof(TService) == typeof(ISpanMappingService))
            {
                if (_spanMappingService == null)
                {
                    lock (_lock)
                    {
                        if (_spanMappingService == null)
                        {
                            _spanMappingService = new RazorSpanMappingService(_document);
                        }
                    }
                }

                return (TService)(object)_spanMappingService; 
            }

            if (typeof(TService) == typeof(IDocumentExcerptService))
            {
                if (_excerptService == null)
                {
                    lock (_lock)
                    {
                        if (_excerptService == null)
                        {
                            _excerptService = new RazorDocumentExcerptService(_document, GetService<ISpanMappingService>());
                        }
                    }
                }

                return (TService)(object)_excerptService;
            }

            return this as TService;
        }
    }
}
