// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Diagnostics.Elm
{
    internal class RequestIdentifier : IDisposable
    {
        private readonly bool _addedFeature;
        private readonly bool _updatedIdentifier;
        private readonly string _originalIdentifierValue;
        private readonly HttpContext _context;
        private readonly IHttpRequestIdentifierFeature _feature;

        private RequestIdentifier(HttpContext context)
        {
            _context = context;
            _feature = context.Features.Get<IHttpRequestIdentifierFeature>();

            if (_feature == null)
            {
                _feature = new HttpRequestIdentifierFeature()
                {
                    TraceIdentifier = Guid.NewGuid().ToString()
                };
                context.Features.Set(_feature);
                _addedFeature = true;
            }
            else if (string.IsNullOrEmpty(_feature.TraceIdentifier))
            {
                _originalIdentifierValue = _feature.TraceIdentifier;
                _feature.TraceIdentifier = Guid.NewGuid().ToString();
                _updatedIdentifier = true;
            }
        }

        public static IDisposable Ensure(HttpContext context)
        {
            return new RequestIdentifier(context);
        }

        public void Dispose()
        {
            if (_addedFeature)
            {
                _context.Features.Set<IHttpRequestIdentifierFeature>(null);
            }
            else if (_updatedIdentifier)
            {
                _feature.TraceIdentifier = _originalIdentifierValue;
            }
        }
    }
}
