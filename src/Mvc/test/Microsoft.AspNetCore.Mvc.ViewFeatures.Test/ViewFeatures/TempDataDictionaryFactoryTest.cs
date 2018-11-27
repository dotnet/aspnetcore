// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class TempDataDictionaryFactoryTest
    {
        [Fact]
        public void Factory_CreatesTempData_ForEachHttpContext()
        {
            // Arrange
            var factory = CreateFactory();

            var context1 = new DefaultHttpContext();
            var context2 = new DefaultHttpContext();

            var tempData1 = factory.GetTempData(context1);

            // Act
            var tempData2 = factory.GetTempData(context2);

            // Assert
            Assert.NotSame(tempData1, tempData2);
        }

        [Fact]
        public void Factory_StoresTempData_InHttpContext()
        {
            // Arrange
            var factory = CreateFactory();

            var context = new DefaultHttpContext();

            var tempData1 = factory.GetTempData(context);

            // Act
            var tempData2 = factory.GetTempData(context);

            // Assert
            Assert.Same(tempData1, tempData2);
        }

        private TempDataDictionaryFactory CreateFactory()
        {
            var provider = new SessionStateTempDataProvider();
            return new TempDataDictionaryFactory(provider);
        }
    }
}
