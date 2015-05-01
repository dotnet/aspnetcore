// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web
{
    public class PassThroughAttribute : AuthorizationFilterAttribute
    {
#pragma warning disable 1998
        public override async Task OnAuthorizationAsync(AuthorizationContext context)
        {
        }
#pragma warning restore 1998
    }
}
