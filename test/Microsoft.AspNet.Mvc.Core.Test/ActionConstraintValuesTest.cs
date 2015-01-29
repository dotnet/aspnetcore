// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class ActionConstraintValuesTest
    {
        [Fact]
        public void IActionConstraintMetadata_InitializesCorrectValues()
        {
            // Arrange
            var constraint = new TestConstraintMetadata();

            // Act
            var constraintValues = new ActionConstraintValues(constraint);

            // Assert
            Assert.False(constraintValues.IsConstraint);
            Assert.False(constraintValues.IsFactory);
            Assert.Equal(0, constraintValues.Order);
            Assert.Equal(typeof(TestConstraintMetadata), constraintValues.ActionConstraintMetadataType);
        }

        [Fact]
        public void IActionConstraint_InitializesCorrectValues()
        {
            // Arrange
            var constraint = new TestConstraint();

            // Act
            var constraintValues = new ActionConstraintValues(constraint);

            // Assert
            Assert.True(constraintValues.IsConstraint);
            Assert.False(constraintValues.IsFactory);
            Assert.Equal(23, constraintValues.Order);
            Assert.Equal(typeof(TestConstraint), constraintValues.ActionConstraintMetadataType);
        }

        [Fact]
        public void IActionConstraintFactory_InitializesCorrectValues()
        {
            // Arrange
            var constraint = new TestFactory();

            // Act
            var constraintValues = new ActionConstraintValues(constraint);

            // Assert
            Assert.False(constraintValues.IsConstraint);
            Assert.True(constraintValues.IsFactory);
            Assert.Equal(0, constraintValues.Order);
            Assert.Equal(typeof(TestFactory), constraintValues.ActionConstraintMetadataType);
        }

        private class TestConstraintMetadata : IActionConstraintMetadata
        {
        }

        private class TestConstraint : IActionConstraint
        {
            public int Order
            {
                get { return 23; }
            }

            public bool Accept(ActionConstraintContext context)
            {
                return false;
            }
        }

        private class TestFactory : IActionConstraintFactory
        {
            public IActionConstraint CreateInstance(IServiceProvider services)
            {
                return new TestConstraint();
            }
        }
    }
}