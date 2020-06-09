// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        public AccessTokenNotAvailableException(
            NavigationManager navigation,
            AccessTokenResult tokenResult,
            IEnumerable<string> scopes)
            : base(message: "Unable to provision an access token for the requested scopes: " +
                  scopes != null ? $"'{string.Join(", ", scopes ?? Array.Empty<string>())}'" : "(default scopes)")
        {
            _tokenResult = tokenResult;
            _navigation = navigation;
        }

        public void Redirect() => _navigation.NavigateTo(_tokenResult.RedirectUrl);
    }
}
