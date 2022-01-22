// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization
{
    // This is so the AuthorizeView can avoid implementing IAuthorizeData (even privately)
    internal class AuthorizeDataAdapter : IAuthorizeData
    {
        private readonly AuthorizeView _component;

        public AuthorizeDataAdapter(AuthorizeView component)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
        }

        public string Policy
        {
            get => _component.Policy;
            set => throw new NotSupportedException();
        }

        public string Roles
        {
            get => _component.Roles;
            set => throw new NotSupportedException();
        }

        // AuthorizeView doesn't expose any such parameter, as it wouldn't be used anyway,
        // since we already have the ClaimsPrincipal by the time AuthorizeView gets involved.
        public string AuthenticationSchemes
        {
            get => null;
            set => throw new NotSupportedException();
        }
    }
}
