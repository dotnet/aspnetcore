// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class DataProtectionRedisTests
    {
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
    }
}
