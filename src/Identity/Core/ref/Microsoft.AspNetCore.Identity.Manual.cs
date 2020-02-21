// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    public partial class SignInManager<TUser> where TUser : class
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task<System.Security.Claims.ClaimsPrincipal> StoreRememberClient(TUser user) { throw null; }
        internal System.Security.Claims.ClaimsPrincipal StoreTwoFactorInfo(string userId, string loginProvider) { throw null; }
        internal partial class TwoFactorAuthenticationInfo
        {
            public TwoFactorAuthenticationInfo() { }
            public string LoginProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
            public string UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        }
    }
}
