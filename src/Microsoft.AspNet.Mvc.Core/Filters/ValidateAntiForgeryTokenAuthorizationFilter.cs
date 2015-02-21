// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ValidateAntiForgeryTokenAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly AntiForgery _antiForgery;

        public ValidateAntiForgeryTokenAuthorizationFilter([NotNull] AntiForgery antiForgery)
        {
            _antiForgery = antiForgery;
        }

        public async Task OnAuthorizationAsync([NotNull] AuthorizationContext context)
        {
            await _antiForgery.ValidateAsync(context.HttpContext);
        }
    }
}