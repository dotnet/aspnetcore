// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.InMemory;

namespace Identity.DefaultUI.WebSite;

public class PocoUserStartup : StartupBase<PocoUser, IdentityDbContext>
{
    public PocoUserStartup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
        });

        services.AddDefaultIdentity<Microsoft.AspNetCore.Identity.Test.PocoUser>()
            .AddUserManager<UserManager<Microsoft.AspNetCore.Identity.Test.PocoUser>>();
        services.AddSingleton<IUserStore<Microsoft.AspNetCore.Identity.Test.PocoUser>, InMemoryUserStore<Microsoft.AspNetCore.Identity.Test.PocoUser>>();

        services.AddMvc()
            .AddNewtonsoftJson();
    }
}
