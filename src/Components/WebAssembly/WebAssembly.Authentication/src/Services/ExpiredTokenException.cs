// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class AccessTokenNotAvailableException : Exception
    {
        private AccessTokenResult _tokenResult;

        public AccessTokenNotAvailableException(AccessTokenResult tokenResult)
            : base(message: "Unable to provision an access token for the requested scopes.") =>
            _tokenResult = tokenResult;

        public void RedirectToLogin() => _tokenResult.TryGetToken(out _, redirect: true);
    }
}
