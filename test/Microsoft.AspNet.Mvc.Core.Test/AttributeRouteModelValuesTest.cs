// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class AttributeRouteModelValuesTest
    {
        [Fact]
        public void AttributeRouteInfoValues_IncludesAllProperties()
        {
            // Assert
            PropertiesAssert.PropertiesAreTheSame(
                typeof(AttributeRouteInfo), 
                typeof(AttributeRouteInfoValues));
        }
    }
}