// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class ConfigurationTests
    {
        private class TestOptions
        {
              public string Message { get; set; }
        }
      
        private class ConfigureTestDefault : ConfigureDefaultOptions<TestOptions>
        {
            public ConfigureTestDefault(IConfiguration config) :
                base(options => config.GetSection("Test").Bind(options))
            { }
        }

        [Fact]
        public void ConfigureAspNetCoreDefaultsEnablesBindAgainstDefaultConfig()
        {
            var dic = new Dictionary<string, string>
            {
                { "Test:Message", "yadayada"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddOptions()
                .AddTransient<ConfigureDefaultOptions<TestOptions>, ConfigureTestDefault>()
                .ConfigureAspNetCoreDefaults();
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptions<TestOptions>>().Value;
            Assert.Equal("yadayada", options.Message);
        }

        [Fact]
        public void DefaultConfigIgnoredWithoutConfigureAspNetCoreDefaults()
        {
            var dic = new Dictionary<string, string>
            {
                { "Test:Message", "yadayada"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddOptions()
                .AddTransient<ConfigureDefaultOptions<TestOptions>, ConfigureTestDefault>();
            var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<IOptions<TestOptions>>().Value;
            Assert.Null(options.Message);
        }

    }
}