// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;

namespace IdentitySample.DefaultUI.Data
{
    public class ApplicationUser : IdentityUser
    {
        [ProtectedPersonalData]
        public string Name { get; set; }
        [PersonalData]
        public int Age { get; set; }
    }
}
