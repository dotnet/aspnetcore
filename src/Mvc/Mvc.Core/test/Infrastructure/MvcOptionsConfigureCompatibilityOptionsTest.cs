// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core.Infrastructure
{
    public class MvcOptionsConfigureCompatibilityOptionsTest
    {
        [Fact]
        public void PostConfigure_ConfiguresMaxValidationDepth()
        {
            // Arrange
            var mvcOptions = new MvcOptions();
            var mvcCompatibilityOptions = new MvcCompatibilityOptions
            {
                CompatibilityVersion = CompatibilityVersion.Version_2_2,
            };

            var configureOptions = new MvcOptionsConfigureCompatibilityOptions(
                NullLoggerFactory.Instance,
                Options.Create(mvcCompatibilityOptions));

            // Act
            configureOptions.PostConfigure(string.Empty, mvcOptions);

            // Assert
            Assert.Equal(32, mvcOptions.MaxValidationDepth);
        }

        [Fact]
        public void PostConfigure_DoesNotConfiguresMaxValidationDepth_WhenSetToNull()
        {
            // Arrange
            var mvcOptions = new MvcOptions
            {
                MaxValidationDepth = null,
            };
            var mvcCompatibilityOptions = new MvcCompatibilityOptions
            {
                CompatibilityVersion = CompatibilityVersion.Version_2_2,
            };

            var configureOptions = new MvcOptionsConfigureCompatibilityOptions(
                NullLoggerFactory.Instance,
                Options.Create(mvcCompatibilityOptions));

            // Act
            configureOptions.PostConfigure(string.Empty, mvcOptions);

            // Assert
            Assert.Null(mvcOptions.MaxValidationDepth);
        }

        [Fact]
        public void PostConfigure_DoesNotConfiguresMaxValidationDepth_WhenSetToValue()
        {
            // Arrange
            var expected = 13;
            var mvcOptions = new MvcOptions
            {
                MaxValidationDepth = expected,
            };
            var mvcCompatibilityOptions = new MvcCompatibilityOptions
            {
                CompatibilityVersion = CompatibilityVersion.Version_2_2,
            };

            var configureOptions = new MvcOptionsConfigureCompatibilityOptions(
                NullLoggerFactory.Instance,
                Options.Create(mvcCompatibilityOptions));

            // Act
            configureOptions.PostConfigure(string.Empty, mvcOptions);

            // Assert
            Assert.Equal(expected, mvcOptions.MaxValidationDepth);
        }
    }
}
