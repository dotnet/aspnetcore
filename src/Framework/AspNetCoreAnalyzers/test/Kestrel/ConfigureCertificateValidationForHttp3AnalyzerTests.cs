// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.Kestrel.ConfigureCertificateValidationForHttp3Analyzer>;

namespace Microsoft.AspNetCore.Analyzers.Kestrel;

public class ConfigureCertificateValidationForHttp3AnalyzerTests
{
    [Fact]
    public async Task ReportsDiagnostic_InlineHttp3CallbackWithoutCustomValidation()
    {
        var source = GetSource(
            "HttpProtocols.Http3",
            """
                        ClientCertificateRequired = true,
                        {|#0:ServerCertificateContext|} = SslStreamCertificateContext.Create(
                            null!,
                            null,
                            false,
                            SslCertificateTrust.CreateForX509Collection([])),
            """);

        await VerifyCS.VerifyAnalyzerAsync(
            source,
            new DiagnosticResult(DiagnosticDescriptors.ConfigureCertificateValidationForHttp3).WithLocation(0));
    }

    [Theory]
    [InlineData("HttpProtocols.Http1AndHttp2")]
    [InlineData("HttpProtocols.Http1AndHttp2AndHttp3")]
    public async Task ReportsOnlyWhenHttp3IsEnabled(string protocols)
    {
        var source = GetSource(
            protocols,
            """
                        ClientCertificateRequired = true,
                        {|#0:ServerCertificateContext|} = SslStreamCertificateContext.Create(
                            null!,
                            null,
                            false,
                            SslCertificateTrust.CreateForX509Collection([])),
            """);

        var expected = protocols == "HttpProtocols.Http1AndHttp2"
            ? []
            : new[] { new DiagnosticResult(DiagnosticDescriptors.ConfigureCertificateValidationForHttp3).WithLocation(0) };
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("ClientCertificateRequired = false,")]
    [InlineData("RemoteCertificateValidationCallback = (_, _, _, _) => true,")]
    [InlineData("CertificateChainPolicy = new(),")]
    public async Task NoDiagnostic_WhenConfigurationIsNotUnsafe(string additionalConfiguration)
    {
        var source = GetSource(
            "HttpProtocols.Http3",
            $$"""
                        {{additionalConfiguration}}
                        ServerCertificateContext = SslStreamCertificateContext.Create(
                            null!,
                            null,
                            false,
                            SslCertificateTrust.CreateForX509Collection([])),
            """);

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTrustArgumentIsNull()
    {
        var source = GetSource(
            "HttpProtocols.Http3",
            """
                        ClientCertificateRequired = true,
                        ServerCertificateContext = SslStreamCertificateContext.Create(null!, null, false, null),
            """);

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoDiagnostic_WhenHttp3IsConfiguredOnDifferentListenOptions()
    {
        var source = GetSource(
            "HttpProtocols.Http1AndHttp2",
            """
                        ClientCertificateRequired = true,
                        ServerCertificateContext = SslStreamCertificateContext.Create(
                            null!,
                            null,
                            false,
                            SslCertificateTrust.CreateForX509Collection([])),
            """,
            "otherListenOptions.Protocols = HttpProtocols.Http3;");

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    private static string GetSource(string protocols, string sslOptionsInitializer, string additionalCode = "")
        => $$"""
using System.Net.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

public static class Configuration
{
    public static void Main()
    {
    }

    public static void Configure(ListenOptions listenOptions, ListenOptions otherListenOptions)
    {
        listenOptions.Protocols = {{protocols}};
        {{additionalCode}}
        listenOptions.UseHttps(new TlsHandshakeCallbackOptions
        {
            OnConnection = context => ValueTask.FromResult(new SslServerAuthenticationOptions
            {
{{sslOptionsInitializer}}
            }),
        });
    }
}
""";
}
