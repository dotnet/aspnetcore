// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class MvcOptionsTests
    {
        [Fact]
        public void AntiForgeryOptions_SettingNullValue_Throws()
        {
            // Arrange
            var options = new MvcOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.AntiForgeryOptions = null);
            Assert.Equal("The 'AntiForgeryOptions' property of 'Microsoft.AspNet.Mvc.MvcOptions' must not be null." +
                         Environment.NewLine + "Parameter name: value", ex.Message);
        }

        [Fact]
        public void MaxValidationError_ThrowsIfValueIsOutOfRange()
        {
            // Arrange
            var options = new MvcOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxModelValidationErrors = -1);
            Assert.Equal("value", ex.ParamName);
        }
    }
}