// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Security.DataProtection;
using Moq;
using Xunit;

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
            return new AntiForgery(claimExtractor.Object,
                                   dataProtectionProvider.Object,
                                   additionalDataProvider.Object);
        }
    }
}
