// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Xunit;
using Moq;

namespace Microsoft.AspNetCore.FunctionalTests
{
    public class CertificateLoaderTests
    {
        [Fact]
        public void Loads_SingleCertificateName_File()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["TestConfig:Certificate"] = "Certificate1"
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
            certificateFileLoader.VerifyAll();
        }

        [Fact]
        public void Throws_SingleCertificateName_File_KeyNotFound()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["TestConfig:Certificate"] = "Certificate2"
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                Mock.Of<ICertificateStoreLoader>());

            var exception = Assert.Throws<InvalidOperationException>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate")));
            Assert.Equal("No certificate named Certificate2 found in configuration", exception.Message);
        }

        [Fact]
        public void Throws_SingleCertificateName_File_FileNotFound()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["TestConfig:Certificate"] = "Certificate1"
                })
                .Build();

            var exception = new Exception();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Callback(() => throw exception);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            Assert.Same(exception, Assert.Throws<Exception>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"))));
        }

        [Fact]
        public void Loads_SingleCertificateName_Store()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "Store",
                    ["Certificates:Certificate1:Subject"] = "localhost",
                    ["Certificates:Certificate1:StoreName"] = "My",
                    ["Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = "Certificate1"
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
            certificateStoreLoader.VerifyAll();
        }

        [Fact]
        public void Throws_SingleCertificateName_Store_KeyNotFound()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "Store",
                    ["Certificates:Certificate1:Subject"] = "localhost",
                    ["Certificates:Certificate1:StoreName"] = "My",
                    ["Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = "Certificate2"
                })
                .Build();

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                Mock.Of<ICertificateStoreLoader>());

            var exception = Assert.Throws<InvalidOperationException>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate")));
            Assert.Equal("No certificate named Certificate2 found in configuration", exception.Message);
        }

        [Fact]
        public void ReturnsNull_SingleCertificateName_Store_NotFoundInStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "Store",
                    ["Certificates:Certificate1:Subject"] = "localhost",
                    ["Certificates:Certificate1:StoreName"] = "My",
                    ["Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = "Certificate1"
                })
                .Build();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns<X509Certificate2>(null);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(0, loadedCertificates.Count());
            certificateStoreLoader.VerifyAll();
        }

        [Fact]
        public void Loads_MultipleCertificateNames_File()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "File",
                    ["Certificates:Certificate2:Path"] = "Certificate2.pfx",
                    ["Certificates:Certificate2:Password"] = "Password2",
                    ["TestConfig:Certificate"] = "Certificate1;Certificate2"
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var certificate2 = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate1);
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate2.pfx", "Password2", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate2);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(2, loadedCertificates.Count());
            Assert.Same(certificate1, loadedCertificates.ElementAt(0));
            Assert.Same(certificate2, loadedCertificates.ElementAt(1));
            certificateFileLoader.VerifyAll();
        }

        [Theory]
        [InlineData("Certificate1;NotFound")]
        [InlineData("Certificate1;Certificate2;NotFound")]
        [InlineData("NotFound;Certificate1")]
        [InlineData("NotFound;Certificate1;Certificate2")]
        [InlineData("Certificate1;NotFound;Certificate2")]
        public void Throws_MultipleCertificateNames_File_KeyNotFound(string certificateNames)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "File",
                    ["Certificates:Certificate2:Path"] = "Certificate2.pfx",
                    ["Certificates:Certificate2:Password"] = "Password2",
                    ["TestConfig:Certificate"] = certificateNames
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var certificate2 = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate1);
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate2.pfx", "Password2", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate2);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            var exception = Assert.Throws<InvalidOperationException>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate")));
            Assert.Equal("No certificate named NotFound found in configuration", exception.Message);
        }

        [Theory]
        [InlineData("Certificate1;Certificate2")]
        [InlineData("Certificate2;Certificate1")]
        public void Throws_MultipleCertificateNames_File_FileNotFound(string certificateNames)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "File",
                    ["Certificates:Certificate2:Path"] = "Certificate2.pfx",
                    ["Certificates:Certificate2:Password"] = "Password2",
                    ["TestConfig:Certificate"] = certificateNames
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var exception = new Exception();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate1);
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate2.pfx", "Password2", It.IsAny<X509KeyStorageFlags>()))
                .Throws(exception);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            Assert.Same(exception, Assert.Throws<Exception>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"))));
        }

        [Fact]
        public void Loads_MultipleCertificateNames_Store()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "Store",
                    ["Certificates:Certificate1:Subject"] = "localhost",
                    ["Certificates:Certificate1:StoreName"] = "My",
                    ["Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "example.com",
                    ["Certificates:Certificate2:StoreName"] = "Root",
                    ["Certificates:Certificate2:StoreLocation"] = "LocalMachine",
                    ["TestConfig:Certificate"] = "Certificate1;Certificate2"
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var certificate2 = new X509Certificate2();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate1);
            certificateStoreLoader
                .Setup(loader => loader.Load("example.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()))
                .Returns(certificate2);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(2, loadedCertificates.Count());
            Assert.Same(certificate1, loadedCertificates.ElementAt(0));
            Assert.Same(certificate2, loadedCertificates.ElementAt(1));
            certificateStoreLoader.VerifyAll();
        }

        [Theory]
        [InlineData("Certificate1;NotFound")]
        [InlineData("Certificate1;Certificate2;NotFound")]
        [InlineData("NotFound;Certificate1")]
        [InlineData("NotFound;Certificate1;Certificate2")]
        [InlineData("Certificate1;NotFound;Certificate2")]
        public void Throws_MultipleCertificateNames_Store_KeyNotFound(string certificateNames)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "Store",
                    ["Certificates:Certificate1:Subject"] = "localhost",
                    ["Certificates:Certificate1:StoreName"] = "My",
                    ["Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "example.com",
                    ["Certificates:Certificate2:StoreName"] = "Root",
                    ["Certificates:Certificate2:StoreLocation"] = "LocalMachine",
                    ["TestConfig:Certificate"] = certificateNames
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var certificate2 = new X509Certificate2();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate1);
            certificateStoreLoader
                .Setup(loader => loader.Load("example.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()))
                .Returns(certificate2);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var exception = Assert.Throws<InvalidOperationException>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate")));
            Assert.Equal("No certificate named NotFound found in configuration", exception.Message);
        }

        [Theory]
        [InlineData("Certificate1;Certificate2", 1)]
        [InlineData("Certificate2;Certificate1", 1)]
        [InlineData("Certificate1;Certificate2;Certificate3", 1)]
        [InlineData("Certificate2;Certificate3", 0)]
        [InlineData("Certificate2;Certificate3;Certificate1", 1)]
        public void ReturnsNull_MultipleCertificateNames_Store_NotFoundInStore(string certificateNames, int expectedFoundCertificates)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "Store",
                    ["Certificates:Certificate1:Subject"] = "localhost",
                    ["Certificates:Certificate1:StoreName"] = "My",
                    ["Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "example.com",
                    ["Certificates:Certificate2:StoreName"] = "Root",
                    ["Certificates:Certificate2:StoreLocation"] = "LocalMachine",
                    ["Certificates:Certificate3:Source"] = "Store",
                    ["Certificates:Certificate3:Subject"] = "notfound.com",
                    ["Certificates:Certificate3:StoreName"] = "Root",
                    ["Certificates:Certificate3:StoreLocation"] = "LocalMachine",
                    ["TestConfig:Certificate"] = certificateNames
                })
                .Build();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(new X509Certificate2());
            certificateStoreLoader
                .Setup(loader => loader.Load("example.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()))
                .Returns<X509Certificate2>(null);
            certificateStoreLoader
                .Setup(loader => loader.Load("notfound.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()))
                .Returns<X509Certificate2>(null);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(expectedFoundCertificates, loadedCertificates.Count());
        }

        [Fact]
        public void Loads_MultipleCertificateNames_FileAndStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "localhost",
                    ["Certificates:Certificate2:StoreName"] = "My",
                    ["Certificates:Certificate2:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = "Certificate1;Certificate2"
                })
                .Build();

            var fileCertificate = new X509Certificate2();
            var storeCertificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(fileCertificate);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(storeCertificate);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(2, loadedCertificates.Count());
            Assert.Same(fileCertificate, loadedCertificates.ElementAt(0));
            Assert.Same(storeCertificate, loadedCertificates.ElementAt(1));
            certificateFileLoader.VerifyAll();
            certificateStoreLoader.VerifyAll();
        }

        [Theory]
        [InlineData("Certificate1;Certificate2;NotFound")]
        [InlineData("Certificate1;NotFound;Certificate2")]
        [InlineData("NotFound;Certificate1;Certificate2")]
        public void Throws_MultipleCertificateNames_FileAndStore_KeyNotFound(string certificateNames)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "localhost",
                    ["Certificates:Certificate2:StoreName"] = "My",
                    ["Certificates:Certificate2:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = certificateNames
                })
                .Build();

            var fileCertificate = new X509Certificate2();
            var storeCertificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(fileCertificate);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(storeCertificate);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            var exception = Assert.Throws<InvalidOperationException>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate")));
            Assert.Equal("No certificate named NotFound found in configuration", exception.Message);
        }

        [Theory]
        [InlineData("Certificate1;Certificate2")]
        [InlineData("Certificate2;Certificate1")]
        public void Throws_MultipleCertificateNames_FileAndStore_FileNotFound(string certificateNames)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "localhost",
                    ["Certificates:Certificate2:StoreName"] = "My",
                    ["Certificates:Certificate2:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = certificateNames
                })
                .Build();

            var exception = new Exception();
            var storeCertificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Throws(exception);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(storeCertificate);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            Assert.Same(exception, Assert.Throws<Exception>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"))));
        }

        [Theory]
        [InlineData("Certificate1;Certificate2")]
        [InlineData("Certificate2;Certificate1")]
        public void ReturnsNull_MultipleCertificateNames_FileAndStore_NotFoundInStore(string certificateNames)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Certificates:Certificate1:Source"] = "File",
                    ["Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["Certificates:Certificate1:Password"] = "Password1",
                    ["Certificates:Certificate2:Source"] = "Store",
                    ["Certificates:Certificate2:Subject"] = "localhost",
                    ["Certificates:Certificate2:StoreName"] = "My",
                    ["Certificates:Certificate2:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificate"] = "Certificate1;Certificate2"
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns<X509Certificate2>(null);

            var certificateLoader = new CertificateLoader(
                configuration.GetSection("Certificates"),
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
            certificateFileLoader.VerifyAll();
            certificateStoreLoader.VerifyAll();
        }

        [Fact]
        public void Loads_SingleCertificateInline_File()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificate:Source"] = "File",
                    ["TestConfig:Certificate:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificate:Password"] = "Password1"
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
            certificateFileLoader.VerifyAll();
        }

        [Fact]
        public void Throws_SingleCertificateInline_FileNotFound()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificate:Source"] = "File",
                    ["TestConfig:Certificate:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificate:Password"] = "Password1"
                })
                .Build();

            var exception = new Exception();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Throws(exception);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            Assert.Same(exception, Assert.Throws<Exception>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"))));
            certificateFileLoader.VerifyAll();
        }

        [Fact]
        public void Loads_SingleCertificateInline_Store()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificate:Source"] = "Store",
                    ["TestConfig:Certificate:Subject"] = "localhost",
                    ["TestConfig:Certificate:StoreName"] = "My",
                    ["TestConfig:Certificate:StoreLocation"] = "CurrentUser",
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate);

            var certificateLoader = new CertificateLoader(
                null,
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
            certificateStoreLoader.VerifyAll();
        }

        [Fact]
        public void ReturnsNull_SingleCertificateInline_Store_NotFoundInStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificate:Source"] = "Store",
                    ["TestConfig:Certificate:Subject"] = "localhost",
                    ["TestConfig:Certificate:StoreName"] = "My",
                    ["TestConfig:Certificate:StoreLocation"] = "CurrentUser",
                })
                .Build();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();

            var certificateLoader = new CertificateLoader(
                null,
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificate"));
            Assert.Equal(0, loadedCertificates.Count());
            certificateStoreLoader.Verify(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()));
        }

        [Fact]
        public void Loads_MultipleCertificatesInline_File()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "File",
                    ["TestConfig:Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificates:Certificate1:Password"] = "Password1",
                    ["TestConfig:Certificates:Certificate2:Source"] = "File",
                    ["TestConfig:Certificates:Certificate2:Path"] = "Certificate2.pfx",
                    ["TestConfig:Certificates:Certificate2:Password"] = "Password2",
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var certificate2 = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate1);
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate2.pfx", "Password2", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate2);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"));
            Assert.Equal(2, loadedCertificates.Count());
            Assert.Same(certificate1, loadedCertificates.ElementAt(0));
            Assert.Same(certificate2, loadedCertificates.ElementAt(1));
            certificateFileLoader.VerifyAll();
        }

        [Fact]
        public void Throws_MultipleCertificatesInline_File_FileNotFound()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "File",
                    ["TestConfig:Certificates:Certificate1:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificates:Certificate1:Password"] = "Password1",
                    ["TestConfig:Certificates:Certificate2:Source"] = "File",
                    ["TestConfig:Certificates:Certificate2:Path"] = "Certificate2.pfx",
                    ["TestConfig:Certificates:Certificate2:Password"] = "Password2",
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var exception = new Exception();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate1);
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate2.pfx", "Password2", It.IsAny<X509KeyStorageFlags>()))
                .Throws(exception);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                Mock.Of<ICertificateStoreLoader>());

            Assert.Same(exception, Assert.Throws<Exception>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"))));
        }

        [Fact]
        public void Loads_MultipleCertificatesInline_Store()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate1:Subject"] = "localhost",
                    ["TestConfig:Certificates:Certificate1:StoreName"] = "My",
                    ["TestConfig:Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificates:Certificate2:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate2:Subject"] = "example.com",
                    ["TestConfig:Certificates:Certificate2:StoreName"] = "Root",
                    ["TestConfig:Certificates:Certificate2:StoreLocation"] = "LocalMachine"
                })
                .Build();

            var certificate1 = new X509Certificate2();
            var certificate2 = new X509Certificate2();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate1);
            certificateStoreLoader
                .Setup(loader => loader.Load("example.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()))
                .Returns(certificate2);

            var certificateLoader = new CertificateLoader(
                null,
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"));
            Assert.Equal(2, loadedCertificates.Count());
            Assert.Same(certificate1, loadedCertificates.ElementAt(0));
            Assert.Same(certificate2, loadedCertificates.ElementAt(1));
            certificateStoreLoader.VerifyAll();
        }

        [Fact]
        public void ReturnsNull_MultipleCertificatesInline_Store_NotFoundInStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate1:Subject"] = "notfound.com",
                    ["TestConfig:Certificates:Certificate1:StoreName"] = "Root",
                    ["TestConfig:Certificates:Certificate1:StoreLocation"] = "LocalMachine",
                    ["TestConfig:Certificates:Certificate2:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate2:Subject"] = "localhost",
                    ["TestConfig:Certificates:Certificate2:StoreName"] = "My",
                    ["TestConfig:Certificates:Certificate2:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificates:Certificate3:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate3:Subject"] = "example.com",
                    ["TestConfig:Certificates:Certificate3:StoreName"] = "Root",
                    ["TestConfig:Certificates:Certificate3:StoreLocation"] = "LocalMachine"
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate);

            var certificateLoader = new CertificateLoader(
                null,
                Mock.Of<ICertificateFileLoader>(),
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
            certificateStoreLoader.Verify(loader => loader.Load("notfound.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()));
            certificateStoreLoader.Verify(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()));
            certificateStoreLoader.Verify(loader => loader.Load("example.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()));
        }

        [Fact]
        public void Loads_MultipleCertificatesInline_FileAndStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate1:Subject"] = "localhost",
                    ["TestConfig:Certificates:Certificate1:StoreName"] = "My",
                    ["TestConfig:Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificates:Certificate2:Source"] = "File",
                    ["TestConfig:Certificates:Certificate2:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificates:Certificate2:Password"] = "Password1",
                    ["TestConfig:Certificates:Certificate3:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate3:Subject"] = "example.com",
                    ["TestConfig:Certificates:Certificate3:StoreName"] = "Root",
                    ["TestConfig:Certificates:Certificate3:StoreLocation"] = "LocalMachine",
                    ["TestConfig:Certificates:Certificate4:Source"] = "File",
                    ["TestConfig:Certificates:Certificate4:Path"] = "Certificate2.pfx",
                    ["TestConfig:Certificates:Certificate4:Password"] = "Password2",
                })
                .Build();

            var fileCertificate1 = new X509Certificate2();
            var fileCertificate2 = new X509Certificate2();
            var storeCertificate1 = new X509Certificate2();
            var storeCertificate2 = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(fileCertificate1);
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate2.pfx", "Password2", It.IsAny<X509KeyStorageFlags>()))
                .Returns(fileCertificate2);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(storeCertificate1);
            certificateStoreLoader
                .Setup(loader => loader.Load("example.com", "Root", StoreLocation.LocalMachine, It.IsAny<bool>()))
                .Returns(storeCertificate2);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"));
            Assert.Equal(4, loadedCertificates.Count());
            Assert.Same(storeCertificate1, loadedCertificates.ElementAt(0));
            Assert.Same(fileCertificate1, loadedCertificates.ElementAt(1));
            Assert.Same(storeCertificate2, loadedCertificates.ElementAt(2));
            Assert.Same(fileCertificate2, loadedCertificates.ElementAt(3));
            certificateStoreLoader.VerifyAll();
        }

        [Fact]
        public void Throws_MultipleCertificatesInline_FileAndStore_FileNotFound()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate1:Subject"] = "localhost",
                    ["TestConfig:Certificates:Certificate1:StoreName"] = "My",
                    ["TestConfig:Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificates:Certificate2:Source"] = "File",
                    ["TestConfig:Certificates:Certificate2:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificates:Certificate2:Password"] = "Password1",
                })
                .Build();

            var exception = new Exception();
            var certificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Throws(exception);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns(certificate);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            Assert.Same(exception, Assert.Throws<Exception>(() => certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"))));
        }

        [Fact]
        public void ReturnsNull_MultipleCertificatesInline_FileAndStore_NotFoundInStore()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["TestConfig:Certificates:Certificate1:Source"] = "Store",
                    ["TestConfig:Certificates:Certificate1:Subject"] = "localhost",
                    ["TestConfig:Certificates:Certificate1:StoreName"] = "My",
                    ["TestConfig:Certificates:Certificate1:StoreLocation"] = "CurrentUser",
                    ["TestConfig:Certificates:Certificate2:Source"] = "File",
                    ["TestConfig:Certificates:Certificate2:Path"] = "Certificate1.pfx",
                    ["TestConfig:Certificates:Certificate2:Password"] = "Password1",
                })
                .Build();

            var certificate = new X509Certificate2();

            var certificateFileLoader = new Mock<ICertificateFileLoader>();
            certificateFileLoader
                .Setup(loader => loader.Load("Certificate1.pfx", "Password1", It.IsAny<X509KeyStorageFlags>()))
                .Returns(certificate);

            var certificateStoreLoader = new Mock<ICertificateStoreLoader>();
            certificateStoreLoader
                .Setup(loader => loader.Load("localhost", "My", StoreLocation.CurrentUser, It.IsAny<bool>()))
                .Returns<X509Certificate>(null);

            var certificateLoader = new CertificateLoader(
                null,
                certificateFileLoader.Object,
                certificateStoreLoader.Object);

            var loadedCertificates = certificateLoader.Load(configuration.GetSection("TestConfig:Certificates"));
            Assert.Equal(1, loadedCertificates.Count());
            Assert.Same(certificate, loadedCertificates.ElementAt(0));
        }
    }
}
