// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Interfaces.Authentication;

namespace Microsoft.AspNet.Http.Core.Authentication
{
    public class SignOutContext : ISignOutContext
    {
        private bool _accepted;

        public SignOutContext(string authenticationScheme)
        {
            AuthenticationScheme = authenticationScheme;
        }

        public string AuthenticationScheme { get; }

        public bool Accepted
        {
            get { return _accepted; }
        }

        public void Accept()
        {
            _accepted = true;
        }
    }
}
