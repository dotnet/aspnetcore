// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests;

public class ConfigurationReaderTests
{
    [Fact]
    public void ReadCertificatesWhenNoCertificatesSection_ReturnsEmptyCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var reader = new ConfigurationReader(config);
        var certificates = reader.Certificates;
        Assert.NotNull(certificates);
        Assert.False(certificates.Any());
    }

    [Fact]
    public void ReadCertificatesWhenEmptyCertificatesSection_ReturnsEmptyCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Certificates", ""),
        }).Build();
        var reader = new ConfigurationReader(config);
        var certificates = reader.Certificates;
        Assert.NotNull(certificates);
        Assert.False(certificates.Any());
    }

    [Fact]
    public void ReadCertificatesSection_ReturnsCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Certificates:FileCert:Path", "/path/cert.pfx"),
            new KeyValuePair<string, string>("Certificates:FileCert:Password", "certpassword"),
            new KeyValuePair<string, string>("Certificates:StoreCert:Subject", "certsubject"),
            new KeyValuePair<string, string>("Certificates:StoreCert:Store", "certstore"),
            new KeyValuePair<string, string>("Certificates:StoreCert:Location", "cetlocation"),
            new KeyValuePair<string, string>("Certificates:StoreCert:AllowInvalid", "true"),
        }).Build();
        var reader = new ConfigurationReader(config);
        var certificates = reader.Certificates;
        Assert.NotNull(certificates);
        Assert.Equal(2, certificates.Count);

        var fileCert = certificates["FileCert"];
        Assert.True(fileCert.IsFileCert);
        Assert.False(fileCert.IsStoreCert);
        Assert.Equal("/path/cert.pfx", fileCert.Path);
        Assert.Equal("certpassword", fileCert.Password);

        var storeCert = certificates["StoreCert"];
        Assert.False(storeCert.IsFileCert);
        Assert.True(storeCert.IsStoreCert);
        Assert.Equal("certsubject", storeCert.Subject);
        Assert.Equal("certstore", storeCert.Store);
        Assert.Equal("cetlocation", storeCert.Location);
        Assert.True(storeCert.AllowInvalid);
    }

    [Fact]
    public void ReadCertificatesSection_IsCaseInsensitive()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Certificates:filecert:Path", "/path/cert.pfx"),
            new KeyValuePair<string, string>("CERTIFICATES:FILECERT:PASSWORD", "certpassword"),
        }).Build();
        var reader = new ConfigurationReader(config);
        var certificates = reader.Certificates;
        Assert.NotNull(certificates);
        Assert.Equal(1, certificates.Count);

        var fileCert = certificates["FiLeCeRt"];
        Assert.True(fileCert.IsFileCert);
        Assert.False(fileCert.IsStoreCert);
        Assert.Equal("/path/cert.pfx", fileCert.Path);
        Assert.Equal("certpassword", fileCert.Password);
    }

    [Fact]
    public void ReadCertificatesSection_ThrowsOnCaseInsensitiveDuplicate()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                    new KeyValuePair<string, string>("Certificates:filecert:Password", "certpassword"),
                    new KeyValuePair<string, string>("Certificates:FILECERT:Password", "certpassword"),
            }).Build());

        Assert.Contains(CoreStrings.KeyAlreadyExists, exception.Message);
    }

    [Fact]
    public void ReadEndpointsWhenNoEndpointsSection_ReturnsEmptyCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var reader = new ConfigurationReader(config);
        var endpoints = reader.Endpoints;
        Assert.NotNull(endpoints);
        Assert.False(endpoints.Any());
    }

    [Fact]
    public void ReadEndpointsWhenEmptyEndpointsSection_ReturnsEmptyCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints", ""),
            }).Build();
        var reader = new ConfigurationReader(config);
        var endpoints = reader.Endpoints;
        Assert.NotNull(endpoints);
        Assert.False(endpoints.Any());
    }

    [Fact]
    public void ReadEndpointWithMissingUrl_Throws()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:End1", ""),
            }).Build();
        var reader = new ConfigurationReader(config);
        Assert.Throws<InvalidOperationException>(() => reader.Endpoints);
    }

    [Fact]
    public void ReadEndpointWithEmptyUrl_Throws()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:End1:Url", ""),
            }).Build();
        var reader = new ConfigurationReader(config);
        Assert.Throws<InvalidOperationException>(() => reader.Endpoints);
    }

    [Fact]
    public void ReadEndpointsSection_ReturnsCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
                new KeyValuePair<string, string>("Endpoints:End2:Url", "https://*:5002"),
                new KeyValuePair<string, string>("Endpoints:End2:ClientCertificateMode", "AllowCertificate"),
                new KeyValuePair<string, string>("Endpoints:End3:Url", "https://*:5003"),
                new KeyValuePair<string, string>("Endpoints:End3:ClientCertificateMode", "RequireCertificate"),
                new KeyValuePair<string, string>("Endpoints:End3:Certificate:Path", "/path/cert.pfx"),
                new KeyValuePair<string, string>("Endpoints:End3:Certificate:Password",  "certpassword"),
                new KeyValuePair<string, string>("Endpoints:End4:Url", "https://*:5004"),
                new KeyValuePair<string, string>("Endpoints:End4:ClientCertificateMode", "NoCertificate"),
                new KeyValuePair<string, string>("Endpoints:End4:Certificate:Subject",  "certsubject"),
                new KeyValuePair<string, string>("Endpoints:End4:Certificate:Store", "certstore"),
                new KeyValuePair<string, string>("Endpoints:End4:Certificate:Location", "cetlocation"),
                new KeyValuePair<string, string>("Endpoints:End4:Certificate:AllowInvalid", "true"),
            }).Build();
        var reader = new ConfigurationReader(config);
        var endpoints = reader.Endpoints;
        Assert.NotNull(endpoints);
        Assert.Equal(4, endpoints.Count());

        var end1 = endpoints.First();
        Assert.Equal("End1", end1.Name);
        Assert.Equal("http://*:5001", end1.Url);
        Assert.Null(end1.ClientCertificateMode);
        Assert.NotNull(end1.ConfigSection);
        Assert.NotNull(end1.Certificate);
        Assert.False(end1.Certificate.ConfigSection.Exists());

        var end2 = endpoints.Skip(1).First();
        Assert.Equal("End2", end2.Name);
        Assert.Equal("https://*:5002", end2.Url);
        Assert.Equal(ClientCertificateMode.AllowCertificate, end2.ClientCertificateMode);
        Assert.NotNull(end2.ConfigSection);
        Assert.NotNull(end2.Certificate);
        Assert.False(end2.Certificate.ConfigSection.Exists());

        var end3 = endpoints.Skip(2).First();
        Assert.Equal("End3", end3.Name);
        Assert.Equal("https://*:5003", end3.Url);
        Assert.Equal(ClientCertificateMode.RequireCertificate, end3.ClientCertificateMode);
        Assert.NotNull(end3.ConfigSection);
        Assert.NotNull(end3.Certificate);
        Assert.True(end3.Certificate.ConfigSection.Exists());
        var cert3 = end3.Certificate;
        Assert.True(cert3.IsFileCert);
        Assert.False(cert3.IsStoreCert);
        Assert.Equal("/path/cert.pfx", cert3.Path);
        Assert.Equal("certpassword", cert3.Password);

        var end4 = endpoints.Skip(3).First();
        Assert.Equal("End4", end4.Name);
        Assert.Equal("https://*:5004", end4.Url);
        Assert.Equal(ClientCertificateMode.NoCertificate, end4.ClientCertificateMode);
        Assert.NotNull(end4.ConfigSection);
        Assert.NotNull(end4.Certificate);
        Assert.True(end4.Certificate.ConfigSection.Exists());
        var cert4 = end4.Certificate;
        Assert.False(cert4.IsFileCert);
        Assert.True(cert4.IsStoreCert);
        Assert.Equal("certsubject", cert4.Subject);
        Assert.Equal("certstore", cert4.Store);
        Assert.Equal("cetlocation", cert4.Location);
        Assert.True(cert4.AllowInvalid);
    }

    [Fact]
    public void ReadEndpointWithSingleSslProtocolSet_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:0", "Tls11"),
        }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.Endpoints.First();
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        Assert.Equal(SslProtocols.Tls11, endpoint.SslProtocols);
#pragma warning restore SYSLIB0039
    }

    [Fact]
    public void ReadEndpointWithMultipleSslProtocolsSet_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:0", "Tls11"),
            new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:1", "Tls12"),
        }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.Endpoints.First();
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        Assert.Equal(SslProtocols.Tls11 | SslProtocols.Tls12, endpoint.SslProtocols);
#pragma warning restore SYSLIB0039
    }

    [Fact]
    public void ReadEndpointWithSslProtocolSet_ReadsCaseInsensitive()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:0", "TLS11"),
        }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.Endpoints.First();
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        Assert.Equal(SslProtocols.Tls11, endpoint.SslProtocols);
#pragma warning restore SYSLIB0039
    }

    [Fact]
    public void ReadEndpointWithNoSslProtocolSettings_ReturnsNull()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
        }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.Endpoints.First();
        Assert.Null(endpoint.SslProtocols);
    }

    [Fact]
    public void ReadEndpointWithEmptySniSection_ReturnsEmptyCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
        }).Build();

        var reader = new ConfigurationReader(config);

        var endpoint = reader.Endpoints.First();
        Assert.NotNull(endpoint.Sni);
        Assert.False(endpoint.Sni.Any());
    }

    [Fact]
    public void ReadEndpointWithEmptySniKey_Throws()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni::Protocols", "Http1"),
        }).Build();

        var reader = new ConfigurationReader(config);
        var end1Ex = Assert.Throws<InvalidOperationException>(() => reader.Endpoints);

        Assert.Equal(CoreStrings.FormatSniNameCannotBeEmpty("End1"), end1Ex.Message);
    }

    [Fact]
    public void ReadEndpointWithSniConfigured_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:Protocols", "Http1"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:SslProtocols:0", "Tls12"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:Certificate:Path", "/path/cert.pfx"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:Certificate:Password", "certpassword"),
            new KeyValuePair<string, string>("Endpoints:End1:SNI:*.example.org:ClientCertificateMode", "AllowCertificate"),
        }).Build();

        var reader = new ConfigurationReader(config);

        static void VerifySniConfig(SniConfig config)
        {
            Assert.NotNull(config);

            Assert.Equal(HttpProtocols.Http1, config.Protocols);
            Assert.Equal(SslProtocols.Tls12, config.SslProtocols);
            Assert.Equal("/path/cert.pfx", config.Certificate.Path);
            Assert.Equal("certpassword", config.Certificate.Password);
            Assert.Equal(ClientCertificateMode.AllowCertificate, config.ClientCertificateMode);
        }

        VerifySniConfig(reader.Endpoints.First().Sni["*.Example.org"]);
    }

    [Fact]
    public void ReadEndpointDefaultsWithSingleSslProtocolSet_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("EndpointDefaults:SslProtocols:0", "Tls11"),
        }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.EndpointDefaults;
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        Assert.Equal(SslProtocols.Tls11, endpoint.SslProtocols);
#pragma warning restore SYSLIB0039
    }

    [Fact]
    public void ReadEndpointDefaultsWithNoSslProtocolSettings_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.EndpointDefaults;
        Assert.Null(endpoint.SslProtocols);
    }

    [Fact]
    public void ReadEndpointWithNoClientCertificateModeSettings_ReturnsNull()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.Endpoints.First();
        Assert.Null(endpoint.ClientCertificateMode);
    }

    [Fact]
    public void ReadEndpointDefaultsWithClientCertificateModeSet_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("EndpointDefaults:ClientCertificateMode", "AllowCertificate"),
            }).Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.EndpointDefaults;
        Assert.Equal(ClientCertificateMode.AllowCertificate, endpoint.ClientCertificateMode);
    }

    [Fact]
    public void ReadEndpointDefaultsWithNoAllowCertificateSettings_ReturnsCorrectValue()
    {
        var config = new ConfigurationBuilder().Build();
        var reader = new ConfigurationReader(config);

        var endpoint = reader.EndpointDefaults;
        Assert.Null(endpoint.ClientCertificateMode);
    }
}
