// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
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
            Assert.Throws<InvalidOperationException>(() => new ConfigurationReader(config));
        }

        [Fact]
        public void ReadEndpointWithEmptyUrl_Throws()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", ""),
            }).Build();
            Assert.Throws<InvalidOperationException>(() => new ConfigurationReader(config));
        }

        [Fact]
        public void ReadEndpointsSection_ReturnsCollection()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
                new KeyValuePair<string, string>("Endpoints:End2:Url", "https://*:5002"),
                new KeyValuePair<string, string>("Endpoints:End3:Url", "https://*:5003"),
                new KeyValuePair<string, string>("Endpoints:End3:Certificate:Path", "/path/cert.pfx"),
                new KeyValuePair<string, string>("Endpoints:End3:Certificate:Password",  "certpassword"),
                new KeyValuePair<string, string>("Endpoints:End4:Url", "https://*:5004"),
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
            Assert.NotNull(end1.ConfigSection);
            Assert.NotNull(end1.Certificate);
            Assert.False(end1.Certificate.ConfigSection.Exists());

            var end2 = endpoints.Skip(1).First();
            Assert.Equal("End2", end2.Name);
            Assert.Equal("https://*:5002", end2.Url);
            Assert.NotNull(end2.ConfigSection);
            Assert.NotNull(end2.Certificate);
            Assert.False(end2.Certificate.ConfigSection.Exists());

            var end3 = endpoints.Skip(2).First();
            Assert.Equal("End3", end3.Name);
            Assert.Equal("https://*:5003", end3.Url);
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
    }
}
