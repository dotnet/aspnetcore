// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiBehaviorOptionsSetupTest
    {
        [Fact]
        public void Configure_AssignsInvalidModelStateResponseFactory()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup(
                NullLoggerFactory.Instance,
                Options.Create(new MvcCompatibilityOptions()));
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Same(ApiBehaviorOptionsSetup.DefaultFactory, options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void Configure_AddsClientErrorMappings()
        {
            // Arrange
            var expected = new[] { 400, 401, 403, 404, 406, 409, 415, 422, };
            var optionsSetup = new ApiBehaviorOptionsSetup(
                NullLoggerFactory.Instance,
                Options.Create(new MvcCompatibilityOptions()));
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);

            // Assert
            Assert.Equal(expected, options.ClientErrorMapping.Keys);
        }

        [Fact]
        public void PostConfigure_SetProblemDetailsModelStateResponseFactory()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup(
                NullLoggerFactory.Instance,
                Options.Create(new MvcCompatibilityOptions { CompatibilityVersion = CompatibilityVersion.Latest }));
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);
            optionsSetup.PostConfigure(string.Empty, options);

            // Assert
            Assert.Same(ApiBehaviorOptionsSetup.ProblemDetailsFactory, options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void PostConfigure_DoesNotSetProblemDetailsFactoryWithLegacyCompatBehavior()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup(
                NullLoggerFactory.Instance,
                Options.Create(new MvcCompatibilityOptions { CompatibilityVersion = CompatibilityVersion.Version_2_1 }));
            var options = new ApiBehaviorOptions();

            // Act
            optionsSetup.Configure(options);
            optionsSetup.PostConfigure(string.Empty, options);

            // Assert
            Assert.Same(ApiBehaviorOptionsSetup.DefaultFactory, options.InvalidModelStateResponseFactory);
        }

        [Fact]
        public void PostConfigure_DoesNotSetProblemDetailsFactory_IfValueWasModified()
        {
            // Arrange
            var optionsSetup = new ApiBehaviorOptionsSetup(
                NullLoggerFactory.Instance,
                Options.Create(new MvcCompatibilityOptions { CompatibilityVersion = CompatibilityVersion.Latest }));
            var options = new ApiBehaviorOptions();
            Func<ActionContext, IActionResult> expected = _ => null;

            // Act
            optionsSetup.Configure(options);
            // This is equivalent to user code updating the value via ConfigureOptions
            options.InvalidModelStateResponseFactory = expected;
            optionsSetup.PostConfigure(string.Empty, options);

            // Assert
            Assert.Same(expected, options.InvalidModelStateResponseFactory);
        }
    }
}
