// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    public interface IAuthenticationManager
    {
        void SignIn(ClaimsIdentity identity, bool isPersistent);
        void SignOut(string authenticationType);

        // remember browser for two factor
        void ForgetClient();
        void RememberClient(string userId);
        Task<bool> IsClientRememeberedAsync(string userId);

        // half cookie
        Task StoreUserId(string userId);
        Task<string> RetrieveUserId();
    }
}