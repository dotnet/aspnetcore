// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdentityOptionsServiceCollectionExtensionsTest
    {
        [Fact]
        public void CanResolveIdentityServiceOptions()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddApplications<IdentityUser, IdentityServiceApplication>(o => { });

            var provider = services.BuildServiceProvider();

            var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
            Assert.NotNull(options.Value);
        }
    }
}
