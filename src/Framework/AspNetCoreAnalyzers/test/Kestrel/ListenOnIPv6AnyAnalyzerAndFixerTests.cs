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

public class ListenOnIPv6AnyAnalyzerAndFixerTests
{
    [Fact]
    public async Task ReportsDiagnostic_IPAddressAsLocalVariable_OuterScope()
    {
        var source = GetKestrelSetupSource("myIp", extraOuterCode: "var myIp = IPAddress.Any;");
        await VerifyCS.VerifyAnalyzerAsync(source, codeSampleDiagnosticResult);
    }

    [Fact]
    public async Task ReportsDiagnostic_IPAddressAsLocalVariable()
    {
        var source = GetKestrelSetupSource("myIp", extraInlineCode: "var myIp = IPAddress.Any;");
        await VerifyCS.VerifyAnalyzerAsync(source, codeSampleDiagnosticResult);
    }

    [Fact]
    public async Task ReportsDiagnostic_ExplicitUsage()
    {
        var source = GetKestrelSetupSource("IPAddress.Any");
        await VerifyCS.VerifyAnalyzerAsync(source, codeSampleDiagnosticResult);
    }

    [Fact]
    public async Task CodeFix_ExplicitUsage()
    {
        var source = GetKestrelSetupSource("IPAddress.Any");
        var fixedSource = GetCorrectedKestrelSetup();
        await VerifyCS.VerifyCodeFixAsync(source, codeSampleDiagnosticResult, fixedSource);
    }

    [Fact]
    public async Task CodeFix_IPAddressAsLocalVariable()
    {
        var source = GetKestrelSetupSource("IPAddress.Any", extraInlineCode: "var myIp = IPAddress.Any;");
        var fixedSource = GetCorrectedKestrelSetup(extraInlineCode: "var myIp = IPAddress.Any;");
        await VerifyCS.VerifyCodeFixAsync(source, codeSampleDiagnosticResult, fixedSource);
    }

    private static DiagnosticResult codeSampleDiagnosticResult
        = new DiagnosticResult(DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny).WithLocation(0);

    static string GetKestrelSetupSource(string ipAddressArgument, string extraInlineCode = null, string extraOuterCode = null)
        => GetCodeSample($$"""Listen({|#0:{{ipAddressArgument}}|}, """, extraInlineCode, extraOuterCode);

    static string GetCorrectedKestrelSetup(string extraInlineCode = null, string extraOuterCode = null)
        => GetCodeSample("ListenAnyIP(", extraInlineCode, extraOuterCode);

    static string GetCodeSample(string invocation, string extraInlineCode = null, string extraOuterCode = null) => $$"""
        using Microsoft.Extensions.Hosting;
        using Microsoft.AspNetCore.Hosting;
        using Microsoft.AspNetCore.Server.Kestrel.Core;
        using System.Net;
    
        {{extraOuterCode}}
    
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseKestrel().ConfigureKestrel(options =>
                {
                    {{extraInlineCode}}
                    
                    options.ListenLocalhost(5000);
                    options.ListenAnyIP(5000);
                    options.{{invocation}}5000, listenOptions =>
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
