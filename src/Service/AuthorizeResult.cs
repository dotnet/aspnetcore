// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizeResult
    {
        private static AuthorizeResult RequireLogin = new AuthorizeResult();

        private AuthorizeResult(AuthorizationRequestError error)
        {
            Error = error;
            Status = AuthorizationStatus.Forbidden;
        }

        private AuthorizeResult(ClaimsPrincipal user, ClaimsPrincipal application)
        {
            User = user;
            Application = application;
            Status = AuthorizationStatus.Authorized;
        }

        private AuthorizeResult()
        {
            Status = AuthorizationStatus.LoginRequired;
        }

        public static AuthorizeResult Forbidden(AuthorizationRequestError error)
        {
            return new AuthorizeResult(error);
        }

        public static AuthorizeResult Authorized(ClaimsPrincipal user, ClaimsPrincipal application)
        {
            return new AuthorizeResult(user, application);
        }

        public static AuthorizeResult LoginRequired() => RequireLogin;

        public AuthorizationStatus Status { get; set; }

        public AuthorizationRequestError Error { get; set; }

        public ClaimsPrincipal User { get; set; }

        public ClaimsPrincipal Application { get; set; }
    }
}
