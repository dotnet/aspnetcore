// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class DefaultActionSelectorSelectAsyncValuesTest
    {       
        [Fact]
        public void EmptyDefaultActionSelectorSelectAsyncValues_ReturnValue()
        {
            // Arrange
            var logObject = new DefaultActionSelectorSelectAsyncValues();

            // Act
            var result = logObject.ToString();

            var expected = "DefaultActionSelector.SelectAsync" + Environment.NewLine +
                "\tActions matching route constraints: " + Environment.NewLine +
                "\tActions matching action constraints: " + Environment.NewLine +
                "\tFinal Matches: " + Environment.NewLine +
                "\tSelected action: No action selected";

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
