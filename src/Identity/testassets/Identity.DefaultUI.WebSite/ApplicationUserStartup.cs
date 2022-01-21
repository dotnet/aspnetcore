// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite.Data;

namespace Identity.DefaultUI.WebSite;

public class ApplicationUserStartup : StartupBase<ApplicationUser, ApplicationDbContext>
{
    public ApplicationUserStartup(IConfiguration configuration) : base(configuration)
    {
    }
}
