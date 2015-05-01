// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class BindingSourceTest
    {
        [Fact]
        public void BindingSource_CanAcceptDataFrom_ThrowsOnComposite()
        {
            // Arrange
            var expected =
                "The provided binding source 'Test Source' is a composite. " +
                "'CanAcceptDataFrom' requires that the source must represent a single type of input." +
                Environment.NewLine +
                "Parameter name: bindingSource";

            var bindingSource = CompositeBindingSource.Create(
                bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
                displayName: "Test Source");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => BindingSource.Query.CanAcceptDataFrom(bindingSource));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void BindingSource_CanAcceptDataFrom_Match()
        {
            // Act
            var result = BindingSource.Query.CanAcceptDataFrom(BindingSource.Query);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void BindingSource_CanAcceptDataFrom_NoMatch()
        {
            // Act
            var result = BindingSource.Query.CanAcceptDataFrom(BindingSource.Path);

            // Assert
            Assert.False(result);
        }
    }
}