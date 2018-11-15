// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Microsoft.AspNetCore.Localization
{
    public class RequestLocalizationOptionsExtensionsTest
    {
        [Fact]
        public void AddInitialRequestCultureProvider_ShouldBeInsertedAtFirstPostion()
        {
            // Arrange
            var options = new RequestLocalizationOptions();
            var provider = new CustomRequestCultureProvider(context => Task.FromResult(new ProviderCultureResult("ar-YE")));

            // Act
            options.AddInitialRequestCultureProvider(provider);

            // Assert
            Assert.Same(provider, options.RequestCultureProviders[0]);
        }
    }
}
