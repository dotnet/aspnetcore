// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Interfaces.Authentication
{
    public interface IAuthenticationHandler
    {
        void GetDescriptions(IDescribeSchemesContext context);

        void Authenticate(IAuthenticateContext context);
        Task AuthenticateAsync(IAuthenticateContext context);

        void Challenge(IChallengeContext context);
        void SignIn(ISignInContext context);
        void SignOut(ISignOutContext context);
    }
}
