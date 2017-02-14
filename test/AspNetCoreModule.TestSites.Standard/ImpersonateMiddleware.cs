// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using System.Threading.Tasks;

namespace AspnetCoreModule.TestSites.Standard
{
    public class ImpersonateMiddleware
    {
        private readonly RequestDelegate next;
        public ImpersonateMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        
        public async Task Invoke(HttpContext context)
        {
            var winIdent = context.User.Identity as WindowsIdentity;
            if (winIdent == null)
            {
                await context.Response.WriteAsync("ImpersonateMiddleware-UserName = NoAuthentication");
                await next.Invoke(context);
            }
            else
            {
                await WindowsIdentity.RunImpersonated(winIdent.AccessToken, async () => {
                    string currentUserName = $"{ WindowsIdentity.GetCurrent().Name}";
                    await context.Response.WriteAsync("ImpersonateMiddleware-UserName = " + currentUserName);
                    await next.Invoke(context);
                });
            }
        } 
    }
}
