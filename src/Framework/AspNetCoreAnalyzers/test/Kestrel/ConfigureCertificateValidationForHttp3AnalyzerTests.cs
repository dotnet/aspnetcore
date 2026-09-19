// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.Kestrel.ConfigureCertificateValidationForHttp3Analyzer>;

namespace Microsoft.AspNetCore.Analyzers.Kestrel;

public class ConfigureCertificateValidationForHttp3AnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnostic_DirectExpressionReturnWithoutValidation()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: true)})");

        await VerifyDiagnosticAsync(source);
    }

    [Fact]
    public async Task ReportsDiagnostic_DirectBlockReturnWithoutValidation()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $$"""
            context =>
            {
                return ValueTask.FromResult({{GetUnsafeOptions(markDiagnostic: true)}});
            }
            """);

        await VerifyDiagnosticAsync(source);
    }

    [Theory]
    [InlineData("(_, _, _, _) => true")]
    [InlineData("(_, _, _, _) => { return true; }")]
    [InlineData("(_, _, _, errors) => { if (errors == SslPolicyErrors.None) { return true; } return true; }")]
    public async Task ReportsDiagnostic_WhenCallbackVisiblyAlwaysReturnsTrue(string callback)
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: true, $"RemoteCertificateValidationCallback = {callback},")})");

        await VerifyDiagnosticAsync(source);
    }

    [Theory]
    [InlineData("(_, _, _, _) => false")]
    [InlineData("(_, _, _, errors) => errors == SslPolicyErrors.None")]
    [InlineData("ValidateCertificate")]
    public async Task NoDiagnostic_WhenCallbackMightValidate(string callback)
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false, $"RemoteCertificateValidationCallback = {callback},")})");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("new X509ChainPolicy()")]
    [InlineData("new X509ChainPolicy { TrustMode = X509ChainTrustMode.System }")]
    [InlineData("new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust }")]
    [InlineData("new X509ChainPolicy { CustomTrustStore = { rootCertificate } }")]
    public async Task ReportsDiagnostic_WhenVisibleChainPolicyDoesNotConfigureCustomRoots(string policy)
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: true, $"CertificateChainPolicy = {policy},")})");

        await VerifyDiagnosticAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenChainPolicyProvesPopulatedCustomRootTrust()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false, """
                CertificateChainPolicy = new X509ChainPolicy
                {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    CustomTrustStore = { rootCertificate },
                },
                """)})");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("customPolicy")]
    [InlineData("CreatePolicy()")]
    [InlineData("new X509ChainPolicy { TrustMode = GetTrustMode(), CustomTrustStore = { rootCertificate } }")]
    public async Task NoDiagnostic_WhenChainPolicyIsOpaque(string policy)
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false, $"CertificateChainPolicy = {policy},")})");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenUnsafeCreationIsNotReturned()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $$"""
            context =>
            {
                var unused = {{GetUnsafeOptions(markDiagnostic: false)}};
                return ValueTask.FromResult(new SslServerAuthenticationOptions());
            }
            """);

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenUnsafeCreationIsInNestedLambda()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $$"""
            context =>
            {
                Func<SslServerAuthenticationOptions> unused = () => {{GetUnsafeOptions(markDiagnostic: false)}};
                return ValueTask.FromResult(new SslServerAuthenticationOptions());
            }
            """);

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(
        "listenOptions.Protocols = HttpProtocols.Http3; listenOptions.Protocols = HttpProtocols.Http1AndHttp2;",
        false)]
    [InlineData(
        "listenOptions.Protocols = HttpProtocols.Http1AndHttp2; listenOptions.Protocols = HttpProtocols.Http3;",
        true)]
    public async Task UsesFinalStraightLineProtocolsAssignment(string protocolConfiguration, bool expectDiagnostic)
    {
        var source = GetSource(
            protocolConfiguration,
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: expectDiagnostic)})");

        if (expectDiagnostic)
        {
            await VerifyDiagnosticAsync(source);
        }
        else
        {
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }

    [Fact]
    public async Task NoDiagnostic_WhenHttp3IsConfiguredOnDifferentListenOptions()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http1AndHttp2; otherListenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false)})");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("""
        if (condition)
        {
            listenOptions.Protocols = HttpProtocols.Http3;
        }
        """)]
    [InlineData("""
        listenOptions.Protocols = HttpProtocols.Http3;
        if (condition)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
        }
        """)]
    public async Task NoDiagnostic_WhenProtocolConfigurationIsConditional(string protocolConfiguration)
    {
        var source = GetSource(
            protocolConfiguration,
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false)})");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTrustArgumentIsNull()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            """
            context => ValueTask.FromResult(new SslServerAuthenticationOptions
            {
                ClientCertificateRequired = true,
                ServerCertificateContext = SslStreamCertificateContext.Create(null!, null, false, null),
            })
            """);

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    private static Task VerifyDiagnosticAsync(string source)
        => VerifyCS.VerifyAnalyzerAsync(
            source,
            new DiagnosticResult(DiagnosticDescriptors.ConfigureCertificateValidationForHttp3).WithLocation(0));

    private static string GetUnsafeOptions(bool markDiagnostic, string additionalConfiguration = "")
    {
        var serverCertificateContext = markDiagnostic
            ? "{|#0:ServerCertificateContext|}"
            : "ServerCertificateContext";

        return $$"""
            new SslServerAuthenticationOptions
            {
                ClientCertificateRequired = true,
                {{additionalConfiguration}}
                {{serverCertificateContext}} = SslStreamCertificateContext.Create(
                    null!,
                    null,
                    false,
                    SslCertificateTrust.CreateForX509Collection([])),
            }
            """;
    }

    private static string GetSource(string protocolConfiguration, string onConnection)
        => $$"""
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

public static class Configuration
{
    private static X509ChainPolicy customPolicy = new();
    private static X509Certificate2 rootCertificate = null!;

    public static void Main()
    {
    }

    public static void Configure(ListenOptions listenOptions, ListenOptions otherListenOptions, bool condition)
    {
        {{protocolConfiguration}}
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            OnConnection = {{onConnection}},
        });
    }

    private static bool ValidateCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
        => errors == SslPolicyErrors.None;

    private static X509ChainPolicy CreatePolicy() => new();

    private static X509ChainTrustMode GetTrustMode() => X509ChainTrustMode.CustomRootTrust;
}
""";
}
