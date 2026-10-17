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

    [Fact]
    public async Task ReportsDiagnostic_InsideListenCallback()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: true)})",
            configureParameters: "",
            configurationStart: "Listen(listenOptions => {",
            configurationEnd: "});");

        await VerifyDiagnosticAsync(source);
    }

    [Theory]
    [InlineData("(_, _, _, _) => true")]
    [InlineData("(_, _, _, _) => { return true; }")]
    [InlineData("(_, _, _, errors) => { if (errors == SslPolicyErrors.None) { return true; } return true; }")]
    [InlineData("delegate { return true; }")]
    [InlineData("(_, _, _, _) => { bool Reject() { return false; } return true; }")]
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
    [InlineData("(_, _, _, errors) => { if (errors != SslPolicyErrors.None) { throw new AuthenticationException(); } return true; }")]
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

    [Fact]
    public async Task NoDiagnostic_WhenUnsafeCreationIsReturnedFromNestedLocalFunction()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $$"""
            context =>
            {
                SslServerAuthenticationOptions CreateOptions() => {{GetUnsafeOptions(markDiagnostic: false)}};
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

    [Fact]
    public async Task NoDiagnostic_WhenTrustFactoryIsOpaqueAndNullable()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false, trustExpression: "GetTrust()")})");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenUseHttpsMethodIsNotKestrelExtension()
    {
        var source = GetSource(
            "listenOptions.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false)})",
            useHttpsInvocationStart: "UseHttps(listenOptions, ");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenHttp3IsConfiguredOnPropertyOfDifferentReceiver()
    {
        var source = GetSource(
            "first.Options.Protocols = HttpProtocols.Http1AndHttp2; second.Options.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: false)})",
            configureParameters: "OptionsHolder first, OptionsHolder second, bool condition",
            useHttpsInvocationStart: "first.Options.UseHttps(");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ReportsDiagnostic_WhenHttp3IsConfiguredOnSamePropertyReceiver()
    {
        var source = GetSource(
            "first.Options.Protocols = HttpProtocols.Http3;",
            $"context => ValueTask.FromResult({GetUnsafeOptions(markDiagnostic: true)})",
            configureParameters: "OptionsHolder first, bool condition",
            useHttpsInvocationStart: "first.Options.UseHttps(");

        await VerifyDiagnosticAsync(source);
    }

    private static Task VerifyDiagnosticAsync(string source)
        => VerifyCS.VerifyAnalyzerAsync(
            source,
            new DiagnosticResult(DiagnosticDescriptors.ConfigureCertificateValidationForHttp3).WithLocation(0));

    private static string GetUnsafeOptions(
        bool markDiagnostic,
        string additionalConfiguration = "",
        string trustExpression = "SslCertificateTrust.CreateForX509Collection([])")
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
                    {{trustExpression}}),
            }
            """;
    }

    private static string GetSource(
        string protocolConfiguration,
        string onConnection,
        string configureParameters = "ListenOptions listenOptions, ListenOptions otherListenOptions, bool condition",
        string useHttpsInvocationStart = "listenOptions.UseHttps(",
        string configurationStart = "",
        string configurationEnd = "")
        => $$"""
using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

public static class Configuration
{
    private static X509ChainPolicy customPolicy = new();
    private static X509Certificate2 rootCertificate = null!;

    private sealed class OptionsHolder
    {
        public ListenOptions Options { get; } = null!;
    }

    public static void Main()
    {
    }

    private static void Configure({{configureParameters}})
    {
        {{configurationStart}}
        {{protocolConfiguration}}
        {{useHttpsInvocationStart}}new TlsHandshakeCallbackOptions
        {
            OnConnection = {{onConnection}},
        });
        {{configurationEnd}}
    }

    private static void Listen(Action<ListenOptions> configure)
    {
    }

    private static void UseHttps(ListenOptions listenOptions, TlsHandshakeCallbackOptions callbackOptions)
    {
    }

    private static bool ValidateCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
        => errors == SslPolicyErrors.None;

    private static SslCertificateTrust? GetTrust() => null;

    private static X509ChainPolicy CreatePolicy() => new();

    private static X509ChainTrustMode GetTrustMode() => X509ChainTrustMode.CustomRootTrust;
}
""";
}
