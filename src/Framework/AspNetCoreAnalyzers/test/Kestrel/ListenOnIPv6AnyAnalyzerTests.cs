// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.Kestrel.ListenOnIPv6AnyAnalyzer,
    Microsoft.AspNetCore.Fixers.Kestrel.ListenOnIPv6AnyFixer>;

namespace Microsoft.AspNetCore.Analyzers.Kestrel;

public class ListenOnIPv6AnyAnalyzerTests
{
    [Fact] // do we need any other scenarios except the direct usage one?
    public async Task ReportsDiagnostic_IPAddressAsLocalVariable()
    {
        var source = GetKestrelSetupSource("myIp", "var myIp = IPAddress.Any;");

        await VerifyCS.VerifyAnalyzerAsync(source, [
            new DiagnosticResult(DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny).WithLocation(0)
        ]);
    }

    [Fact]
    public async Task ReportsDiagnostic_ExplicitUsage()
    {
        var source = GetKestrelSetupSource("IPAddress.Any");

        await VerifyCS.VerifyAnalyzerAsync(source, [
            new DiagnosticResult(DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny).WithLocation(0)
        ]);
    }

    static string GetKestrelSetupSource(string ipAddressArgument, string extraInlineCode = null) => $$"""
        using Microsoft.Extensions.Hosting;
        using Microsoft.AspNetCore.Hosting;
        using Microsoft.AspNetCore.Server.Kestrel.Core;
        using System.Net;
    
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseKestrel().ConfigureKestrel(options =>
                {
                    {{extraInlineCode}}
                    
                    options.ListenLocalhost(5000);
                    options.ListenAnyIP(5000);
                    options.Listen({|#0:{{ipAddressArgument}}|}, 5000, listenOptions =>
                    {
                        listenOptions.UseHttps();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    });
                });
            });
    
        var host = hostBuilder.Build();
        host.Run();
    """;
}
