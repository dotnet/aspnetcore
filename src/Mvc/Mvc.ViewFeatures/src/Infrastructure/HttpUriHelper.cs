// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class HttpUriHelper : UriHelperBase
    {
        private HttpContext _context;

        public HttpUriHelper()
        {
        }

        public void InitializeState(HttpContext context)
        {
            _context = context;
            InitializeState();
        }

        protected override void InitializeState()
        {
            if (_context == null)
            {
                throw new InvalidOperationException($"'{typeof(HttpUriHelper)}' not initialized.");
            }
            SetAbsoluteBaseUri(GetContextBaseUri());
            SetAbsoluteUri(GetFullUri());
        }

        private string GetFullUri()
        {
            var request = _context.Request;
            return UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);
        }

        private string GetContextBaseUri()
        {
            var request = _context.Request;
            return UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            // For now throw as we don't have a good way of aborting the request from here.
            throw new InvalidOperationException(
                "Redirects are not supported on a prerendering environment.");
        }
    }
}
