// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class ConfigTests
    {
        [Fact]
        public void AddCanBindAgainstDefaultConfig()
        {
            var dic = new Dictionary<string, string>
            {
                {"Microsoft:AspNetCore:Authentication:DefaultSignInScheme", "<signin>"},
                {"Microsoft:AspNetCore:Authentication:DefaultAuthenticateScheme", "<auth>"},
                {"Microsoft:AspNetCore:Authentication:DefaultChallengeScheme", "<challenge>"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection()
                .AddOptions()
                .AddSingleton<IConfigureOptions<AuthenticationOptions>, ConfigureDefaults<AuthenticationOptions>>()
                .AddAuthenticationCore()
                .AddSingleton<IConfiguration>(config);
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
            Assert.Equal("<auth>", options.DefaultAuthenticateScheme);
            Assert.Equal("<challenge>", options.DefaultChallengeScheme);
            Assert.Equal("<signin>", options.DefaultSignInScheme);
        }
    }
}
