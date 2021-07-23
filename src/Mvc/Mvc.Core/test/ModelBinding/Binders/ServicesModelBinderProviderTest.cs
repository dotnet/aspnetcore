// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
