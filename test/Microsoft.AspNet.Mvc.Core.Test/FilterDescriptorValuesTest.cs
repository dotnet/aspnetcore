// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Logging;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test.Logging
{
    public class FilterDescriptorValuesTest
    {
        [Fact]
        public void FilterDescriptorValues_IncludesAllProperties()
        {
            // Assert
            PropertiesAssert.PropertiesAreTheSame(
                typeof(FilterDescriptor), 
                typeof(FilterDescriptorValues));
        }
    }
}