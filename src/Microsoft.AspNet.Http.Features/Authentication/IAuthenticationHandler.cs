// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Features.Authentication
{
    public interface IAuthenticationHandler
    {
        void GetDescriptions(DescribeSchemesContext context);

        void Authenticate(AuthenticateContext context);

        Task AuthenticateAsync(AuthenticateContext context);

        void Challenge(ChallengeContext context);

        void SignIn(SignInContext context);

        void SignOut(SignOutContext context);
    }
}
