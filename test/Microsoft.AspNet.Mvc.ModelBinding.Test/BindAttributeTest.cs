// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class BindAttributeTest
    {
        [Fact]
        public void PrefixPropertyDefaultsToNull()
        {
            // Arrange
            BindAttribute attr = new BindAttribute();

            // Act & assert
            Assert.Null(attr.Prefix);
        }
    }
}
