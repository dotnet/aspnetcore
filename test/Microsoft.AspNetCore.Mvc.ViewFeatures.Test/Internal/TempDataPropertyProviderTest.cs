// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class TempDataPropertyProviderTest
    {
        [Fact]
        public void LoadAndTrackChanges_SetsPropertyValue()
        {
            // Arrange
            var provider = new TempDataPropertyProvider();

            var tempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
            tempData["TempDataProperty-TestString"] = "Value";
            tempData.Save();

            var controller = new TestController()
            {
                TempData = tempData,
            };

            // Act
            provider.LoadAndTrackChanges(controller, controller.TempData);

            // Assert
            Assert.Equal("Value", controller.TestString);
            Assert.Null(controller.TestString2);
        }

        [Fact]
        public void LoadAndTrackChanges_ThrowsInvalidOperationException_PrivateSetter()
        {
            // Arrange
            var provider = new TempDataPropertyProvider();

            var tempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
            tempData["TempDataProperty-Test"] = "Value";
            tempData.Save();

            var controller = new TestController_PrivateSet()
            {
                TempData = tempData,
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                provider.LoadAndTrackChanges(controller, controller.TempData));

            Assert.Equal("TempData properties must have a public getter and setter.", exception.Message);
        }

        [Fact]
        public void LoadAndTrackChanges_ThrowsInvalidOperationException_NonPrimitiveType()
        {
            // Arrange
            var provider = new TempDataPropertyProvider();

            var tempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
            tempData["TempDataProperty-Test"] = new object();
            tempData.Save();

            var controller = new TestController_NonPrimitiveType()
            {
                TempData = tempData,
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                provider.LoadAndTrackChanges(controller, controller.TempData));

            Assert.Equal("TempData properties must be declared as primitive types or string only.", exception.Message);
        }

        public class TestController : Controller
        {
            [TempData]
            public string TestString { get; set; }

            [TempData]
            public string TestString2 { get; set; }
        }

        public class TestController_PrivateSet : Controller
        {
            [TempData]
            public string Test { get; private set; }
        }

        public class TestController_NonPrimitiveType : Controller
        {
            [TempData]
            public object Test { get; set; }
        }

        private class NullTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return null;
            }

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }
    }
}
