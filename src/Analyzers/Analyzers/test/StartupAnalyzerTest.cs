// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using VerifyCS = Microsoft.AspNetCore.Analyzers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.StartupAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.AspNetCore.Analyzers
{
    public class StartupAnalyzerTest
    {
        [Fact]
        public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_Standard()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class StartupSignatures_Standard
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {

        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_MoreVariety()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class StartupSignatures_MoreVariety
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void ConfigureServices(IServiceCollection services, StringBuilder s) // Ignored 
        {
        }

        public void Configure(StringBuilder s) // Ignored, 
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        public void ConfigureProduction(IWebHostEnvironment env, IApplicationBuilder app)
        {
        }

        private void Configure(IApplicationBuilder app)  // Ignored
        {
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvc_FindsEndpointRoutingDisabled()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithDefaultRouteAndEndpointRoutingDisabled
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => options.EnableEndpointRouting = false);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_AddMvcOptions_FindsEndpointRoutingDisabled()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithDefaultRouteAndAddMvcOptionsEndpointRoutingDisabled
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddMvcOptions(options => options.EnableEndpointRouting = false);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvc_FindsEndpointRoutingEnabled()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvc
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|MVC1005:app.UseMvc()|};
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvcAndConfiguredRoutes_FindsEndpointRoutingEnabled()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcAndConfiguredRoutes
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|MVC1005:app.UseMvc(routes =>
            {
                routes.MapRoute(""Name"", ""Template"");
            })|};
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvcWithDefaultRoute_FindsEndpointRoutingEnabled()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithDefaultRoute
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|MVC1005:app.UseMvcWithDefaultRoute()|};
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_MultipleMiddleware()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcWithOtherMiddleware
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseMiddleware<AuthorizationMiddleware>();

            {|MVC1005:app.UseMvc()|};

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
            });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_MultipleUseMvc()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcMultiple
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            {|MVC1005:app.UseMvcWithDefaultRoute()|};

            app.UseStaticFiles();
            app.UseMiddleware<AuthorizationMiddleware>();

            {|MVC1005:app.UseMvc()|};

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
            });

            {|MVC1005:app.UseMvc()|};
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_ServicesAnalysis_CallBuildServiceProvider()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class ConfigureServices_BuildServiceProvider
    {
        public void ConfigureServices(IServiceCollection services)
        {
            {|ASP0000:services.BuildServiceProvider()|};
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthorizationConfiguredCorrectly_ReportsNoDiagnostics()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthConfiguredCorrectly
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthorizationConfiguredAsAChain_ReportsNoDiagnostics()
        {
            // Regression test for https://github.com/dotnet/aspnetcore/issues/15203
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest {
    public class UseAuthConfiguredCorrectlyChained
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting()
               .UseAuthorization()
               .UseEndpoints(r => { });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthorizationInvokedMultipleTimesInEndpointRoutingBlock_ReportsNoDiagnostics()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthMultipleTimes
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthorizationConfiguredBeforeUseRouting_ReportsDiagnostics()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthBeforeUseRouting
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer();
            {|ASP0001:app.UseAuthorization()|};
            app.UseRouting();
            app.UseEndpoints(r => { });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthorizationConfiguredBeforeUseRoutingChained_ReportsDiagnostics()
        {
            // This one asserts a false negative for https://github.com/dotnet/aspnetcore/issues/15203.
            // We don't correctly identify chained calls, this test verifies the behavior.
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest {
    public class UseAuthBeforeUseRoutingChained
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer()
               .UseAuthorization()
               .UseRouting()
               .UseEndpoints(r => { });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_UseAuthorizationConfiguredAfterUseEndpoints_ReportsDiagnostics()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthAfterUseEndpoints
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(r => { });
            {|ASP0001:app.UseAuthorization()|};
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task StartupAnalyzer_MultipleUseAuthorization_ReportsNoDiagnostics()
        {
            var source = @"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class UseAuthFallbackPolicy
    {
        public void Configure(IApplicationBuilder app)
        {
            // This sort of setup would be useful if the user wants to use Auth for non-endpoint content to be handled using the Fallback policy, while
            // using the second instance for regular endpoint routing based auth. We do not want to produce a warning in this case.
            app.UseAuthorization();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(r => { });
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }
    }
}
