// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class ActionDescriptorValuesTest
    {
        [Fact]
        public void ActionDescriptorValues_IncludesAllProperties()
        {
            // Arrange
            var include = new[] { "HttpMethods" };

            // Assert
            PropertiesAssert.PropertiesAreTheSame(
                typeof(ControllerActionDescriptor), 
                typeof(ActionDescriptorValues),
                include: include);
        }
    }
}