// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class AbsoluteUrlFactory : IAbsoluteUrlFactory
    {
        public AbsoluteUrlFactory(IHttpContextAccessor httpContextAccessor)
        {
            // We need the context accessor here in order to produce an absolute url from a potentially relative url.
            ContextAccessor = httpContextAccessor;
        }

        public IHttpContextAccessor ContextAccessor { get; }

        // Call this method when you are overriding a service that doesn't have an HttpContext instance available.
        public string GetAbsoluteUrl(string path)
        {
            var (process, result) = ShouldProcessPath(path);
            if (!process)
            {
                return result;
            }

            if (ContextAccessor.HttpContext?.Request == null)
            {
                throw new InvalidOperationException("The request is not currently available. This service can only be used within the context of an existing HTTP request.");
            }

            return GetAbsoluteUrl(ContextAccessor.HttpContext, path);
        }

        // Call this method when you are implementing a service that has an HttpContext instance available.
        public string GetAbsoluteUrl(HttpContext context, string path)
        {
            var (process, result) = ShouldProcessPath(path);
            if (!process)
            {
                return result;
            }
            var request = context.Request;
            return $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase.ToUriComponent()}{path}";
        }

        private (bool, string) ShouldProcessPath(string path)
        {
            if (path == null || !Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
            {
                return (false, null);
            }

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                return (false, path);
            }

            return (true, path);
        }
    }
}
