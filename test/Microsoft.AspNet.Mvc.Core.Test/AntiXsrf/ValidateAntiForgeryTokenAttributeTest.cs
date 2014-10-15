// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Security.DataProtection;
using Moq;
using Xunit;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ValidateAntiForgeryTokenAttributeTest
    {
        [Fact]
        public void ValidationAttribute_ForwardsCallToValidateAntiForgeryTokenAuthorizationFilter()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<AntiForgery>(GetAntiForgeryInstance());
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var attribute = new ValidateAntiForgeryTokenAttribute();

            // Act
            var filter = attribute.CreateInstance(serviceProvider);

            // Assert
            var validationFilter = filter as ValidateAntiForgeryTokenAuthorizationFilter;
            Assert.NotNull(validationFilter);
        }

        private AntiForgery GetAntiForgeryInstance()
        {
            var claimExtractor = new Mock<IClaimUidExtractor>();
            var dataProtectionProvider = new Mock<IDataProtectionProvider>();
            var additionalDataProvider = new Mock<IAntiForgeryAdditionalDataProvider>();
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options).Returns(new MvcOptions());
            return new AntiForgery(claimExtractor.Object,
                                   dataProtectionProvider.Object,
                                   additionalDataProvider.Object,
                                   optionsAccessor.Object);
        }
    }
}
