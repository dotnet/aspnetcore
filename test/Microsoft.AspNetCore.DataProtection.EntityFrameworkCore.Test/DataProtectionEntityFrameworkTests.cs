// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Test;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection
{
    public class DataProtectionEntityFrameworkTests
    {
        [Fact]
        public void CreateRepository_ThrowsIf_ContextIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(null));
        }

        [Fact]
        public void StoreElement_PersistsData()
        {
            var element = XElement.Parse("<Element1/>");
            var friendlyName = "Element1";
            var key = new DataProtectionKey() { FriendlyName = friendlyName, Xml = element.ToString() };
            using (var context = BuildDataProtectionKeyContext(nameof(StoreElement_PersistsData)))
            {
                var service = new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(() => context);
                service.StoreElement(element, friendlyName);
            }
            // Use a separate instance of the context to verify correct data was saved to database
            using (var context = BuildDataProtectionKeyContext(nameof(StoreElement_PersistsData)))
            {
                Assert.Equal(1, context.DataProtectionKeys.Count());
                Assert.Equal(key.FriendlyName, context.DataProtectionKeys.Single()?.FriendlyName);
                Assert.Equal(key.Xml, context.DataProtectionKeys.Single()?.Xml);
            }
        }

        [Fact]
        public void GetAllElements_ReturnsAllElements()
        {
            var element1 = XElement.Parse("<Element1/>");
            var element2 = XElement.Parse("<Element2/>");
            using (var context = BuildDataProtectionKeyContext(nameof(GetAllElements_ReturnsAllElements)))
            {
                var service = new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(() => context);
                service.StoreElement(element1, "element1");
                service.StoreElement(element2, "element2");
            }
            // Use a separate instance of the context to verify correct data was saved to database
            using (var context = BuildDataProtectionKeyContext(nameof(GetAllElements_ReturnsAllElements)))
            {
                var service = new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(() => context);
                var elements = service.GetAllElements();
                Assert.Equal(2, elements.Count);
            }
        }

        private DbContextOptions<DataProtectionKeyContext> BuildDbContextOptions(string databaseName)
            => new DbContextOptionsBuilder<DataProtectionKeyContext>().UseInMemoryDatabase(databaseName: databaseName).Options;

        private DataProtectionKeyContext BuildDataProtectionKeyContext(string databaseName)
            => new DataProtectionKeyContext(BuildDbContextOptions(databaseName));
    }
}
