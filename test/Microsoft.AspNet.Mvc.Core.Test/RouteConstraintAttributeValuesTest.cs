// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class RouteConstraintAttributeValuesTest
    {
        [Fact]
        public void RouteConstraintAttributeValues_IncludesAllProperties()
        {
            // Arrange
            var exclude = new[] { "TypeId" };

            // Assert
            PropertiesAssert.PropertiesAreTheSame(
                typeof(RouteConstraintAttribute), 
                typeof(RouteConstraintAttributeValues), 
                exclude);
        }
    }
}