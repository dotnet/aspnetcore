// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Components
{
    internal class PendingAuthenticationState : IAuthenticationState
    {
        private ClaimsPrincipal _user;

        public ClaimsPrincipal User
        {
            get
            {
                if (_user == null)
                {
                    _user = new ClaimsPrincipal(new ClaimsIdentity());
                }

                return _user;
            }
        }
    }
}
