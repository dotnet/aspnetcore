// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApplicationModels;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class ApiExplorerModelValuesTest
    {
        [Fact]
        public void ApiExplorerModelValues_IncludesAllProperties()
        {
            // Assert
            PropertiesAssert.PropertiesAreTheSame(
                typeof(ApiExplorerModel),
                typeof(ApiExplorerModelValues));
        }
    }
}