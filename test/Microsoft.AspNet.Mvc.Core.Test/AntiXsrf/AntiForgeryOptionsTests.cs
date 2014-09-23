// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class AntiForgeryOptionsTests
    {
        [Fact]
        public void CookieName_SettingNullValue_Throws()
        {
            // Arrange
            var options = new AntiForgeryOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.CookieName = null);
            Assert.Equal("The 'CookieName' property of 'Microsoft.AspNet.Mvc.AntiForgeryOptions' must not be null." + 
                         Environment.NewLine + "Parameter name: value", ex.Message);
        }

        [Fact]
        public void FormFieldName_SettingNullValue_Throws()
        {
            // Arrange
            var options = new AntiForgeryOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => options.FormFieldName = null);
            Assert.Equal("The 'FormFieldName' property of 'Microsoft.AspNet.Mvc.AntiForgeryOptions' must not be null." +
                         Environment.NewLine + "Parameter name: value", ex.Message);
        }
    }
}