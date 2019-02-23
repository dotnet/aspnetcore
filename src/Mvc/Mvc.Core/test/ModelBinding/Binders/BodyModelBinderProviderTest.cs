// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class BodyModelBinderProviderTest
    {
        public static TheoryData<BindingSource> NonBodyBindingSources
        {
            get
            {
                return new TheoryData<BindingSource>()
                {
                    BindingSource.Header,
                    BindingSource.Form,
                    null,
                };
            }
        }

        [Theory]
        [MemberData(nameof(NonBodyBindingSources))]
        public void GetBinder_WhenBindingSourceIsNotFromBody_ReturnsNull(BindingSource source)
        {
            // Arrange
            var provider = CreateProvider();

            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BindingSource = source;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBinder_WhenNoInputFormatters_Throws()
        {
            // Arrange
            var expected = $"'{typeof(MvcOptions).FullName}.{nameof(MvcOptions.InputFormatters)}' must not be empty. " +
                $"At least one '{typeof(IInputFormatter).FullName}' is required to bind from the body.";
            var provider = CreateProvider();
            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BindingSource = BindingSource.Body;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => provider.GetBinder(context));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void GetBinder_WhenBindingSourceIsFromBody_ReturnsBinder()
        {
            // Arrange
            var provider = CreateProvider(new TestInputFormatter());
            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BindingSource = BindingSource.Body;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<BodyModelBinder>(result);
        }

        [Fact]
        public void GetBinder_DoesNotThrowNullReferenceException()
        {
            // Arrange
            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BindingSource = BindingSource.Body;
            var formatter = new TestInputFormatter();
            var formatterList = new List<IInputFormatter> { formatter };
            var provider = new BodyModelBinderProvider(formatterList, new TestHttpRequestStreamReaderFactory());

            // Act & Assert (does not throw)
            provider.GetBinder(context);
        }

        private static BodyModelBinderProvider CreateProvider(params IInputFormatter[] formatters)
        {
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            return new BodyModelBinderProvider(
                new List<IInputFormatter>(formatters),
                new TestHttpRequestStreamReaderFactory(),
                loggerFactory);
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private class TestInputFormatter : IInputFormatter
        {
            public bool CanRead(InputFormatterContext context)
            {
                throw new NotImplementedException();
            }

            public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
