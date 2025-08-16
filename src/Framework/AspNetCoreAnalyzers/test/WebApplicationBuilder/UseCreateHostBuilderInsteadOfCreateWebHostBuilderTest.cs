// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyAnalyzer = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.UseCreateHostBuilderInsteadOfCreateWebHostBuilderAnalyzer>;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.UseCreateHostBuilderInsteadOfCreateWebHostBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers.UseCreateHostBuilderInsteadOfCreateWebHostBuilderFixer>;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public class UseCreateHostBuilderInsteadOfCreateWebHostBuilderTest
{
    [Fact]
    public async Task DoesNotWarnWhenUsingHostCreateDefaultBuilder()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { });
}
";
        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task WarnsWhenUsingWebHostCreateDefaultBuilder()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static {|#0:IWebHostBuilder|} CreateWebHostBuilder(string[] args) =>
        {|#1:WebHost.CreateDefaultBuilder(args)|}
            .UseStartup<Startup>();
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Hosting;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateWebHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
public class Startup { }
";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder)
            .WithMessage(Resources.Analyzer_UseCreateHostBuilderInsteadOfCreateWebHostBuilder_Message);

        var expectedDiagnostics = new[]
        {
            diagnostic.WithLocation(0),
            diagnostic.WithLocation(1),
        };

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenUsingCreateWebHostBuilderWithBlockBody()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static {|#0:IWebHostBuilder|} CreateWebHostBuilder(string[] args)
    {
        return {|#1:WebHost.CreateDefaultBuilder(args)|}.UseStartup<Startup>();
    }
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Hosting;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
public class Startup { }
";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder)
            .WithMessage(Resources.Analyzer_UseCreateHostBuilderInsteadOfCreateWebHostBuilder_Message);

        var expectedDiagnostics = new[]
        {
            diagnostic.WithLocation(0),
            diagnostic.WithLocation(1),
        };

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task WarnsOnlyForWebHostCreateDefaultBuilder()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static {|#0:IWebHostBuilder|} CreateWebHostBuilder(string[] args) =>
        {|#1:WebHost.CreateDefaultBuilder(args)|}.UseStartup<Startup>();
    
    public static void StartWeb() =>
        WebHost.Start(""http://localhost:5000"", (c) => { });
}
public class Startup { }
";

        var expectedDiagnostics = new[]
        {
            VerifyAnalyzer.Diagnostic().WithLocation(0),
            VerifyAnalyzer.Diagnostic().WithLocation(1)
        };

        await VerifyAnalyzer.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task WarnsForMultipleMethods()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static {|#0:IWebHostBuilder|} CreateWebHostBuilder(string[] args) =>
        {|#1:WebHost.CreateDefaultBuilder(args)|}.UseStartup<Startup>();
    
    public static {|#2:IWebHostBuilder|} CreateWebHostBuilderForTesting(string[] args) =>
        {|#3:WebHost.CreateDefaultBuilder(args)|}.UseStartup<Startup>();
}
public class Startup { }
";

        var expectedDiagnostics = new[]
        {
            VerifyAnalyzer.Diagnostic().WithLocation(0),
            VerifyAnalyzer.Diagnostic().WithLocation(1),
            VerifyAnalyzer.Diagnostic().WithLocation(2),
            VerifyAnalyzer.Diagnostic().WithLocation(3)
        };

        await VerifyAnalyzer.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task WarnsForInstanceMethod()
    {
        // Arrange  
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public class Program
{
    public static void Main(string[] args)
    {
    }

    public {|#0:IWebHostBuilder|} CreateWebHostBuilder(string[] args) =>
        {|#1:WebHost.CreateDefaultBuilder(args)|}.UseStartup<Startup>();
}
public class Startup { }
";

        var expectedDiagnostics = new[]
        {
            VerifyAnalyzer.Diagnostic().WithLocation(0),
            VerifyAnalyzer.Diagnostic().WithLocation(1)
        };

        // No diagnostics expected
        await VerifyAnalyzer.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task WarnsForPrivateMethods()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    private static {|#0:IWebHostBuilder|} CreateWebHostBuilder(string[] args) =>
        {|#1:WebHost.CreateDefaultBuilder(args)|}.UseStartup<Startup>();
}
public class Startup { }
";

        var expectedDiagnostics = new[]
        {
            VerifyAnalyzer.Diagnostic().WithLocation(0),
            VerifyAnalyzer.Diagnostic().WithLocation(1)
        };

        // No diagnostics expected
        await VerifyAnalyzer.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task CodeFixWorksInsideUsingStatement()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        using (var host = {|#0:WebHost.CreateDefaultBuilder(args)|}
                .UseStartup<Startup>()
                .Build())
        {
            host.Run();
        }
    }
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Hosting;

public static class Program
{
    public static void Main(string[] args)
    {
        using (var host = Host.CreateDefaultBuilder(args)
.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>()
).Build())
        {
            host.Run();
        }
    }
}
public class Startup { }
";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder)
            .WithMessage(Resources.Analyzer_UseCreateHostBuilderInsteadOfCreateWebHostBuilder_Message);

        var expectedDiagnostics = new[]
        {
            diagnostic.WithLocation(0),
        };

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task CodeFixWorksWithManyChainedCalls()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
public static class Program
{
    public static void Main(string[] args)
    {
        {|#0:WebHost.CreateDefaultBuilder(new[] { ""--cliKey"", ""cliValue"" })|}
            .ConfigureServices((context, service) => { })
            .ConfigureKestrel(options =>
                options.Configure(options.ConfigurationLoader.Configuration))
            .Configure(app =>
            {
            });
    }
}
";

        var fixedSource = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Hosting;

public static class Program
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(new[] { ""--cliKey"", ""cliValue"" })
.ConfigureWebHostDefaults(webBuilder => webBuilder.ConfigureServices((context, service) => { })
.ConfigureKestrel(options =>
                options.Configure(options.ConfigurationLoader.Configuration))
.Configure(app =>
            {
            }));
    }
}
";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder)
            .WithMessage(Resources.Analyzer_UseCreateHostBuilderInsteadOfCreateWebHostBuilder_Message);

        var expectedDiagnostics = new[]
        {
            diagnostic.WithLocation(0),
        };

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task DoesNotWarnForIWebHostBuilderMethodWithoutWebHostUsage()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) => null!;
}
";
        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}
