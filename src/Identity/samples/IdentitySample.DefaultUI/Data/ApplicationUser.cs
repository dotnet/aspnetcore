// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;

namespace IdentitySample.DefaultUI.Data;

public class ApplicationUser : IdentityUser
{
    [ProtectedPersonalData]
    public string Name { get; set; }
    [PersonalData]
    public int Age { get; set; }
}
