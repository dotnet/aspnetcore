// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("settings.json");
            var config = builder.Build();

            var store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var cert = store.Certificates.Find(X509FindType.FindByThumbprint, config["CertificateThumbprint"], false);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            serviceCollection.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("."))
                .ProtectKeysWithAzureKeyVault(config["KeyId"], config["ClientId"], cert.OfType<X509Certificate2>().Single());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            var protector = serviceProvider.GetDataProtector("Test");

            Console.WriteLine(protector.Protect("Hello world"));
        }
    }
}
