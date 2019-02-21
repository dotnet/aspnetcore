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
            // For now throw.
            // We don't have a good way to do redirects on a prerrendering context.
            // Currently there's no good way to stop the rendering process. We can fix
            // this by passing in a cancellation token to RenderRootComponentAsync and
            // having that checked before we await on every ProcessAsynchronousWork iteration
            // if we choose to go that way. Upon redirect we could trigger the cancellation here
            // and do _context.Response.Redirect(uri)
            // The bigger (not easily solvable problem) is that the response might have already
            // started (for example when the page/view has already been flushed) which will mean
            // that the behavior of this feature is not reliable, and not easily to diagnose as
            // the developer only notices it when this happens at runtime; something we don't really want.
            throw new InvalidOperationException(
                "Redirects are not supported on a prerrendering environment.");

            // We need to do something here about the hash to prevent infinite redirects
            // as the hash portion of a url doesn't get sent to the browser.
            // This means that the hash won't be respected for prerrendering scenarios.
            // There are several involved things that we could do. Bake the knowledge into
            // the IUriHelper abstraction so that we can produce a special link where we
            // encode the hash as a query stirng parameter.
            // We would preprocess this on the server to set the appropriate url.
            // For example: 
            // CurrentUrl = "http://www.localhost/base/path?query=value#top
            // Redirect = "http://www.localhost/base/path?query=value#first
        }
    }
}
