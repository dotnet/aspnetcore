// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.DataProtection
{
    public static partial class StackExchangeRedisDataProtectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToStackExchangeRedis(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, StackExchange.Redis.IConnectionMultiplexer connectionMultiplexer) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToStackExchangeRedis(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, StackExchange.Redis.IConnectionMultiplexer connectionMultiplexer, StackExchange.Redis.RedisKey key) { throw null; }
        public static Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder PersistKeysToStackExchangeRedis(this Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder builder, System.Func<StackExchange.Redis.IDatabase> databaseFactory, StackExchange.Redis.RedisKey key) { throw null; }
    }
}
namespace Microsoft.AspNetCore.DataProtection.StackExchangeRedis
{
    public partial class RedisXmlRepository : Microsoft.AspNetCore.DataProtection.Repositories.IXmlRepository
    {
        public RedisXmlRepository(System.Func<StackExchange.Redis.IDatabase> databaseFactory, StackExchange.Redis.RedisKey key) { }
        public System.Collections.Generic.IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() { throw null; }
        public void StoreElement(System.Xml.Linq.XElement element, string friendlyName) { }
    }
}
