// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// An <see cref="Exception"/> that is thrown when an <see cref="AuthorizationMessageHandler"/> instance
    /// is not able to provision an access token.
    /// </summary>
    public class AccessTokenNotAvailableException : Exception
    {
        private readonly NavigationManager _navigation;
        private readonly AccessTokenResult _tokenResult;

        public AccessTokenNotAvailableException(AccessTokenResult tokenResult, NavigationManager navigation)
            : base(message: "Unable to provision an access token for the requested scopes.") =>
            (_tokenResult, _navigation) = (tokenResult, navigation);

        public void RedirectToLogin() => _navigation.NavigateTo(_tokenResult.RedirectUrl);
    }
}
