// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace BasicTestApp.AuthTest
{
    // DTO shared between server and client
    public class ClientSideAuthenticationStateData
    {
        public bool IsAuthenticated { get; set; }

        public string UserName { get; set; }

        public List<(string Type, string Value)> ExposedClaims { get; set; }
    }
}
