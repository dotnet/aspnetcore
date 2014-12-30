// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestUser
    {
        public TestUser()
        {
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; private set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}