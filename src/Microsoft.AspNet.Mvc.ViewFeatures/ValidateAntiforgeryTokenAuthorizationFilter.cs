// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ValidateAntiforgeryTokenAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IAntiforgery _antiforgery;

        public ValidateAntiforgeryTokenAuthorizationFilter([NotNull] IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        public Task OnAuthorizationAsync([NotNull] AuthorizationContext context)
        {
            return _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
    }
}