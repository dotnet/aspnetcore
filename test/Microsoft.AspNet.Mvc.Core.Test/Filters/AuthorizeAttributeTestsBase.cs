// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Http;
using Moq;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class AuthorizeAttributeTestsBase
    {
        protected AuthorizationContext GetAuthorizationContext(Action<ServiceCollection> registerServices, bool anonymous = false)
        {
            var validUser = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[] { 
                        new Claim("Permission", "CanViewPage"),
                        new Claim(ClaimTypes.Role, "Administrator"), 
                        new Claim(ClaimTypes.NameIdentifier, "John")},
                        "Basic"));

            // ServiceProvider
            var serviceCollection = new ServiceCollection();
            if (registerServices != null)
            {
                registerServices(serviceCollection);
            }

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // HttpContext
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.User).Returns(anonymous ? null : validUser);
            httpContext.SetupGet(c => c.RequestServices).Returns(serviceProvider);

            // AuthorizationContext
            var actionContext = new ActionContext(
                httpContext: httpContext.Object,
                router: null,
                routeValues: null,
                actionDescriptor: null
                );

            var authorizationContext = new AuthorizationContext(
                actionContext,
                Enumerable.Empty<IFilter>().ToList()
            );

            return authorizationContext;
        }
    }
}