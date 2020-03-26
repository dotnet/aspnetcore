// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.StackExchangeRedis
{
    public class RedisDataProtectionBuilderExtensionsTest
    {
        [Fact]
        public void PersistKeysToRedis_UsesRedisXmlRepository()
        {
            // Arrange
            var connection = Mock.Of<IConnectionMultiplexer>();
            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddDataProtection();

            // Act
            builder.PersistKeysToStackExchangeRedis(connection);
            var services = serviceCollection.BuildServiceProvider();

            // Assert
            var options = services.GetRequiredService<IOptions<KeyManagementOptions>>();
            Assert.IsType<RedisXmlRepository>(options.Value.XmlRepository);
        }
    }
}
