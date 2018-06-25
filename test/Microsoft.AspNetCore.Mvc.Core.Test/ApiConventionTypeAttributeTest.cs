// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ApiConventionTypeAttributeTest
    {
        [Fact]
        public void Constructor_ThrowsIfConventionMethodIsAnnotatedWithProducesAttribute()
        {
            // Arrange
            var expected = $"Method {typeof(ConventionWithProducesAttribute).FullName + ".Get"} is decorated with the following attributes that are not allowed on an API convention method:" +
                Environment.NewLine +
                typeof(ProducesAttribute).FullName +
                Environment.NewLine +
                $"The following attributes are allowed on API convention methods: {nameof(ProducesResponseTypeAttribute)}, {nameof(ApiConventionNameMatchAttribute)}";

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => new ApiConventionTypeAttribute(typeof(ConventionWithProducesAttribute)),
                "conventionType",
                expected);
        }

        public static class ConventionWithProducesAttribute
        {
            [Produces(typeof(void))]
            public static void Get() { }
        }

        [Fact]
        public void Constructor_ThrowsIfConventionMethodHasRouteAttribute()
        {
            // Arrange
            var expected = $"Method {typeof(ConventionWithRouteAttribute).FullName + ".Get"} is decorated with the following attributes that are not allowed on an API convention method:" +
                Environment.NewLine +
                typeof(HttpGetAttribute).FullName +
                Environment.NewLine +
                $"The following attributes are allowed on API convention methods: {nameof(ProducesResponseTypeAttribute)}, {nameof(ApiConventionNameMatchAttribute)}";

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => new ApiConventionTypeAttribute(typeof(ConventionWithRouteAttribute)),
                "conventionType",
                expected);
        }

        public static class ConventionWithRouteAttribute
        {
            [HttpGet("url")]
            public static void Get() { }
        }

        [Fact]
        public void Constructor_ThrowsIfMultipleUnsupportedAttributesArePresentOnConvention()
        {
            // Arrange
            var expected = $"Method {typeof(ConventionWitUnsupportedAttributes).FullName + ".Get"} is decorated with the following attributes that are not allowed on an API convention method:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, typeof(ProducesAttribute).FullName, typeof(ServiceFilterAttribute).FullName, typeof(AuthorizeAttribute).FullName) +
                Environment.NewLine +
                $"The following attributes are allowed on API convention methods: {nameof(ProducesResponseTypeAttribute)}, {nameof(ApiConventionNameMatchAttribute)}";

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => new ApiConventionTypeAttribute(typeof(ConventionWitUnsupportedAttributes)),
                "conventionType",
                expected);
        }

        public static class ConventionWitUnsupportedAttributes
        {
            [ProducesResponseType(400)]
            [Produces(typeof(void))]
            [ServiceFilter(typeof(object))]
            [Authorize]
            public static void Get() { }
        }
    }
}
