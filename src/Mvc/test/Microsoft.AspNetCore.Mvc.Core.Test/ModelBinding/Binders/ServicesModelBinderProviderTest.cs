// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class ServicesModelBinderProviderTest
    {
        public static TheoryData<BindingSource> NonServicesBindingSources
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
        [MemberData(nameof(NonServicesBindingSources))]
        public void Create_WhenBindingSourceIsNotFromServices_ReturnsNull(BindingSource source)
        {
            // Arrange
            var provider = new ServicesModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(IPersonService));
            context.BindingInfo.BindingSource = source;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_WhenBindingSourceIsFromServices_ReturnsBinder()
        {
            // Arrange
            var provider = new ServicesModelBinderProvider();

            var context = new TestModelBinderProviderContext(typeof(IPersonService));
            context.BindingInfo.BindingSource = BindingSource.Services;

            // Act
            var result = provider.GetBinder(context);

            // Assert
            Assert.IsType<ServicesModelBinder>(result);
        }

        private class IPersonService
        {
        }
    }
}
