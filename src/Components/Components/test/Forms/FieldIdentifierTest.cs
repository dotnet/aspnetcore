// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Forms;
using System;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Forms
{
    public class FieldIdentifierTest
    {
        [Fact]
        public void CannotUseNullModel()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new FieldIdentifier(null, "somefield"));
            Assert.Equal("model", ex.ParamName);
        }

        [Fact]
        public void CannotUseValueTypeModel()
        {
            var ex = Assert.Throws<ArgumentException>(() => new FieldIdentifier(DateTime.Now, "somefield"));
            Assert.Equal("model", ex.ParamName);
            Assert.StartsWith("The model must be a reference-typed object.", ex.Message);
        }

        [Fact]
        public void CannotUseNullFieldName()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new FieldIdentifier(new object(), null));
            Assert.Equal("fieldName", ex.ParamName);
        }

        [Fact]
        public void CanUseEmptyFieldName()
        {
            var fieldIdentifier = new FieldIdentifier(new object(), string.Empty);
            Assert.Equal(string.Empty, fieldIdentifier.FieldName);
        }

        [Fact]
        public void CanGetModelAndFieldName()
        {
            // Arrange/Act
            var model = new object();
            var fieldIdentifier = new FieldIdentifier(model, "someField");

            // Assert
            Assert.Same(model, fieldIdentifier.Model);
            Assert.Equal("someField", fieldIdentifier.FieldName);
        }

        [Fact]
        public void DistinctModelsProduceDistinctHashCodesAndNonEquality()
        {
            // Arrange
            var fieldIdentifier1 = new FieldIdentifier(new object(), "field");
            var fieldIdentifier2 = new FieldIdentifier(new object(), "field");

            // Act/Assert
            Assert.NotEqual(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
            Assert.False(fieldIdentifier1.Equals(fieldIdentifier2));
        }

        [Fact]
        public void DistinctFieldNamesProduceDistinctHashCodesAndNonEquality()
        {
            // Arrange
            var model = new object();
            var fieldIdentifier1 = new FieldIdentifier(model, "field1");
            var fieldIdentifier2 = new FieldIdentifier(model, "field2");

            // Act/Assert
            Assert.NotEqual(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
            Assert.False(fieldIdentifier1.Equals(fieldIdentifier2));
        }

        [Fact]
        public void SameContentsProduceSameHashCodesAndEquality()
        {
            // Arrange
            var model = new object();
            var fieldIdentifier1 = new FieldIdentifier(model, "field");
            var fieldIdentifier2 = new FieldIdentifier(model, "field");

            // Act/Assert
            Assert.Equal(fieldIdentifier1.GetHashCode(), fieldIdentifier2.GetHashCode());
            Assert.True(fieldIdentifier1.Equals(fieldIdentifier2));
        }

        [Fact]
        public void FieldNamesAreCaseSensitive()
        {
            // Arrange
            var model = new object();
            var fieldIdentifierLower = new FieldIdentifier(model, "field");
            var fieldIdentifierPascal = new FieldIdentifier(model, "Field");

            // Act/Assert
            Assert.Equal("field", fieldIdentifierLower.FieldName);
            Assert.Equal("Field", fieldIdentifierPascal.FieldName);
            Assert.NotEqual(fieldIdentifierLower.GetHashCode(), fieldIdentifierPascal.GetHashCode());
            Assert.False(fieldIdentifierLower.Equals(fieldIdentifierPascal));
        }
    }
}
