// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public void Create_WhenBindingSourceIsNotFromBody_ReturnsNull(BindingSource source)
        {
            // Arrange
            var provider = new BodyModelBinderProvider(new TestHttpRequestStreamReaderFactory());

            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BindingSource = source;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_WhenBindingSourceIsFromBody_ReturnsBinder()
        {
            // Arrange
            var provider = new BodyModelBinderProvider(new TestHttpRequestStreamReaderFactory());

            var context = new TestModelBinderProviderContext(typeof(Person));
            context.BindingInfo.BindingSource = BindingSource.Body;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<BodyModelBinder>(result);
        }

        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
