// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Reflection;
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

        protected override void ConfigureApplication(MvcWebApplicationBuilder<TStartup> builder)
        {
            builder.UseRequestCulture("en-GB", "en-US");            
            builder.ApplicationAssemblies.Clear();
            builder.ApplicationAssemblies.Add(typeof(TStartup).GetTypeInfo().Assembly);
        }

        protected override TestServer CreateServer(MvcWebApplicationBuilder<TStartup> builder)
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
