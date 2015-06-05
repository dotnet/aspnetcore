// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Authorization
{
    public class MockAuthorizationOptionsAccessor : IOptions<AuthorizationOptions>
    {
        public AuthorizationOptions Options { get; } = new AuthorizationOptions();

        public AuthorizationOptions GetNamedOptions(string name)
        {
            return Options;
        }
    }
}