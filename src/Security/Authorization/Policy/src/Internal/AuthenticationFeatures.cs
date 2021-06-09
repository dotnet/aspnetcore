// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Authorization.Policy.Internal
{
    /// <summary>
    /// Keeps the User and AuthenticationResult consistent with each other
    /// </summary>
    internal class AuthenticationFeatures : IAuthenticateResultFeature, IHttpAuthenticationFeature
    {
        public AuthenticationFeatures(AuthenticateResult result)
        {
            Result = result;
        }

        public AuthenticateResult Result { get; set; }

        public ClaimsPrincipal? User
        {
            get => Result.Principal;
            set
            {
                if (value is not null)
                {
                    Result = AuthenticateResult.Success(
                        new AuthenticationTicket(value, Result.Ticket!.Properties, Result.Ticket.AuthenticationScheme));
                }
                else
                {
                    // REVIEW: Make Result nullable and null it out here?
                    throw new ArgumentNullException(nameof(User));
                }
            }
        }
    }
}
