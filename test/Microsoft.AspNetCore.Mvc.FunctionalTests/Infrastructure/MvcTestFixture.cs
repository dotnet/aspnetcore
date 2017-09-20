// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class MvcTestFixture<TStartup> : WebApplicationTestFixture<TStartup>
        where TStartup : class
    {
        public MvcTestFixture()
            : base(Path.Combine("test", "WebSites", typeof(TStartup).Assembly.GetName().Name))
        {
        }

        protected MvcTestFixture(string solutionRelativePath)
            : base(solutionRelativePath)
        {
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseRequestCulture<TStartup>("en-GB", "en-US");

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-GB");
                CultureInfo.CurrentUICulture = new CultureInfo("en-US");
                return base.CreateServer(builder);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }
    }
}
