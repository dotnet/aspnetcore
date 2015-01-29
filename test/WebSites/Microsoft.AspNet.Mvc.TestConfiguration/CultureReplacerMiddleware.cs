// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Testing;

namespace Microsoft.AspNet.Mvc.TestConfiguration
{
    /// <summary>
    /// A middleware that ensures web sites run in a consistent culture. Currently useful for tests that format dates,
    /// times, or numbers. Will be more useful when we have localized resources.
    /// </summary>
    public class CultureReplacerMiddleware
    {
        // Have no current need to use cultures other than the ReplaceCultureAttribute defaults (en-GB, en-US).
        private readonly ReplaceCultureAttribute _replaceCulture = new ReplaceCultureAttribute();

        private readonly RequestDelegate _next;

        public CultureReplacerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Use ReplaceCultureAttribute to avoid thread consistency checks in CultureReplacer. await doesn't
            // necessarily end on the original thread. For this case, problems arise when next middleware throws. Can
            // remove the thread consistency checks once culture is (at least for .NET 4.6) handled using
            // AsyncLocal<CultureInfo>.
            try
            {
                _replaceCulture.Before(methodUnderTest: null);
                await _next(context);
            }
            finally
            {
                _replaceCulture.After(methodUnderTest: null);
            }
        }
    }
}