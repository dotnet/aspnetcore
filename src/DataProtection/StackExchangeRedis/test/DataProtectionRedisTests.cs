// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Testing;
using Moq;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.StackExchangeRedis
{
    public class DataProtectionRedisTests
    {
        private readonly ITestOutputHelper _output;

        public DataProtectionRedisTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GetAllElements_ReturnsAllXmlValuesForGivenKey()
        {
            var database = new Mock<IDatabase>();
            database.Setup(d => d.ListRange("Key", 0, -1, CommandFlags.None)).Returns(new RedisValue[]
            {
                "<Element1/>",
                "<Element2/>",
            }).Verifiable();
            var repo = new RedisXmlRepository(() => database.Object, "Key");

            var elements = repo.GetAllElements().ToArray();

            database.Verify();
            Assert.Equal(new XElement("Element1").ToString(), elements[0].ToString());
            Assert.Equal(new XElement("Element2").ToString(), elements[1].ToString());
        }

        [Fact]
        public void GetAllElements_ThrowsParsingException()
        {
            var database = new Mock<IDatabase>();
            database.Setup(d => d.ListRange("Key", 0, -1, CommandFlags.None)).Returns(new RedisValue[]
            {
                "<Element1/>",
                "<Element2",
            }).Verifiable();
            var repo = new RedisXmlRepository(() => database.Object, "Key");

            Assert.Throws<XmlException>(() => repo.GetAllElements());
        }

        [Fact]
        public void StoreElement_PushesValueToList()
        {
            var database = new Mock<IDatabase>();
            database.Setup(d => d.ListRightPush("Key", "<Element2 />", When.Always, CommandFlags.None)).Verifiable();
            var repo = new RedisXmlRepository(() => database.Object, "Key");

            repo.StoreElement(new XElement("Element2"), null);

            database.Verify();
        }

        [ConditionalFact]
        [TestRedisServerIsAvailable]
        public async Task XmlRoundTripsToActualRedisServer()
        {
            var connStr = TestRedisServer.GetConnectionString();

            _output.WriteLine("Attempting to connect to " + connStr);

            var guid = Guid.NewGuid().ToString();
            RedisKey key = "Test:DP:Key" + guid;

            try
            {
                using (var redis = await ConnectionMultiplexer.ConnectAsync(connStr).TimeoutAfter(TimeSpan.FromMinutes(1)))
                {
                    var repo = new RedisXmlRepository(() => redis.GetDatabase(), key);
                    var element = new XElement("HelloRedis", guid);
                    repo.StoreElement(element, guid);
                }

                using (var redis = await ConnectionMultiplexer.ConnectAsync(connStr).TimeoutAfter(TimeSpan.FromMinutes(1)))
                {
                    var repo = new RedisXmlRepository(() => redis.GetDatabase(), key);
                    var elements = repo.GetAllElements();

                    Assert.Contains(elements, e => e.Name == "HelloRedis" && e.Value == guid);
                }
            }
            finally
            {
                // cleanup
                using (var redis = await ConnectionMultiplexer.ConnectAsync(connStr).TimeoutAfter(TimeSpan.FromMinutes(1)))
                {
                    await redis.GetDatabase().KeyDeleteAsync(key);
                }
            }

        }
    }
}
