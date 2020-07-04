// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DataHandler
{
    public class SecureDataFormatTests
    {
        public SecureDataFormatTests()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public IServiceProvider ServiceProvider { get; }

        [Fact]
        public void ProtectDataRoundTrips()
        {
            var provider = ServiceProvider.GetRequiredService<IDataProtectionProvider>();
            var prototector = provider.CreateProtector("test");
            var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), prototector);

            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            var protectedData = secureDataFormat.Protect(input);
            var result = secureDataFormat.Unprotect(protectedData);
            Assert.Equal(input, result);
        }

        [Fact]
        public void ProtectWithPurposeRoundTrips()
        {
            var provider = ServiceProvider.GetRequiredService<IDataProtectionProvider>();
            var prototector = provider.CreateProtector("test");
            var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), prototector);

            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            string purpose = "purpose1";
            var protectedData = secureDataFormat.Protect(input, purpose);
            var result = secureDataFormat.Unprotect(protectedData, purpose);
            Assert.Equal(input, result);
        }

        [Fact]
        public void UnprotectWithDifferentPurposeFails()
        {
            var provider = ServiceProvider.GetRequiredService<IDataProtectionProvider>();
            var prototector = provider.CreateProtector("test");
            var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), prototector);

            string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            string purpose = "purpose1";
            var protectedData = secureDataFormat.Protect(input, purpose);
            var result = secureDataFormat.Unprotect(protectedData); // Null other purpose
            Assert.Null(result);

            result = secureDataFormat.Unprotect(protectedData, "purpose2");
            Assert.Null(result);
        }

        private class StringSerializer : IDataSerializer<string>
        {
            public byte[] Serialize(string model)
            {
                return Encoding.UTF8.GetBytes(model);
            }

            public string Deserialize(byte[] data)
            {
                return Encoding.UTF8.GetString(data);
            }
        }
    }
}
