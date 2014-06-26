// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface IAuthenticateContext
    {
        IEnumerable<string> AuthenticationTypes { get; }

        void Authenticated(ClaimsIdentity identity, IDictionary<string, string> properties, IDictionary<string, object> description);

        void NotAuthenticated(string authenticationType, IDictionary<string, string> properties, IDictionary<string, object> description);
    }
}
