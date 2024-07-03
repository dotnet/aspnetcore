// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class MvcEncodedTestFixture<TStartup> : MvcTestFixture<TStartup>
    where TStartup : class
{
    public MvcEncodedTestFixture(ILoggerFactory outputHelper) : base(outputHelper) { }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.TryAddTransient<HtmlEncoder, HtmlTestEncoder>();
            services.TryAddTransient<JavaScriptEncoder, JavaScriptTestEncoder>();
            services.TryAddTransient<UrlEncoder, UrlTestEncoder>();
        });
    }
}
