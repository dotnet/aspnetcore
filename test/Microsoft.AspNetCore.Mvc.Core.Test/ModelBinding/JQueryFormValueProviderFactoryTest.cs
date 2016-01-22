// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class JQueryFormValueProviderFactoryTest
    {
        private static readonly Dictionary<string, StringValues> _backingStore = new Dictionary<string, StringValues>
        {
            { "[]", new[] { "found" } },
            { "[]property1", new[] { "found" } },
            { "property2[]", new[] { "found" } },
            { "[]property3[]", new[] { "found" } },
            { "property[]Value", new[] { "found" } },
            { "[10]", new[] { "found" } },
            { "[11]property", new[] { "found" } },
            { "property4[10]", new[] { "found" } },
            { "[12]property[][13]", new[] { "found" } },
            { "[14][]property1[15]property2", new[] { "found" } },
            { "prefix[11]property1", new[] { "found" } },
            { "prefix[12][][property2]", new[] { "found" } },
            { "prefix[property1][13]", new[] { "found" } },
            { "prefix[14][][15]", new[] { "found" } },
            { "[property5][]", new[] { "found" } },
            { "[][property6]Value", new[] { "found" } },
            { "prefix[property2]", new[] { "found" } },
            { "prefix[][property]Value", new[] { "found" } },
            { "[property7][property8]", new[] { "found" } },
            { "[property9][][property10]Value", new[] { "found" } },
        };

        [Fact]
        public async Task GetValueProvider_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
        {
            // Arrange
            var context = CreateContext("some-content-type", formValues: null);
            var factory = new JQueryFormValueProviderFactory();

            // Act
            var result = await factory.GetValueProviderAsync(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("application/x-www-form-urlencoded;charset=utf-8")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq")]
        [InlineData("multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq; charset=utf-8")]
        public async Task GetValueProviderAsync_ReturnsValueProvider_WithCurrentCulture(string contentType)
        {
            // Arrange
            var context = CreateContext(contentType, formValues: null);
            var factory = new JQueryFormValueProviderFactory();

            // Act
            var result = await factory.GetValueProviderAsync(context);

            // Assert
            var valueProvider = Assert.IsType<JQueryFormValueProvider>(result);
            Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
        }

        public static TheoryData<string> SuccessDataSet
        {
            get
            {
                return new TheoryData<string>
                {
                    string.Empty,
                    "property1",
                    "property2",
                    "property3",
                    "propertyValue",
                    "[10]",
                    "[11]property",
                    "property4[10]",
                    "[12]property[13]",
                    "[14]property1[15]property2",
                    "prefix.property1[13]",
                    "prefix[14][15]",
                    ".property5",
                    ".property6Value",
                    "prefix.property2",
                    "prefix.propertyValue",
                    ".property7.property8",
                    ".property9.property10Value",
                };
            }
        }

        [Theory]
        [MemberData(nameof(SuccessDataSet))]
        public async Task GetValueProvider_ReturnsValueProvider_ContainingExpectedKeys(string key)
        {
            // Arrange
            var context = CreateContext("application/x-www-form-urlencoded", formValues: _backingStore);
            var factory = new JQueryFormValueProviderFactory();

            // Act
            var valueProvider = await factory.GetValueProviderAsync(context);
            var result = valueProvider.GetValue(key);

            // Assert
            Assert.Equal("found", (string)result);
        }

        private static ActionContext CreateContext(string contentType, Dictionary<string, StringValues> formValues)
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;

            if (context.Request.HasFormContentType)
            {
                context.Request.Form = new FormCollection(formValues ?? new Dictionary<string, StringValues>());
            }

            return new ActionContext(context, new RouteData(), new ActionDescriptor());
        }
    }
}
