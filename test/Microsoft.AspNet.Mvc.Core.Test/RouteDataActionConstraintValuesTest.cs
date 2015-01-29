// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class RouteDataActionConstraintValuesTest
    {
        [Fact]
        public void RouteDataActionConstraintValues_IncludesAllProperties()
        {
            // Assert
            PropertiesAssert.PropertiesAreTheSame(
                typeof(RouteDataActionConstraint), 
                typeof(RouteDataActionConstraintValues));
        }
    }
}