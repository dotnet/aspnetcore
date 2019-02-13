// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Forms;
using System;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Forms
{
    public class EditContextDataAnnotationsExtensionsTest
    {
        [Fact]
        public void CannotUseNullEditContext()
        {
            var editContext = (EditContext)null;
            var ex = Assert.Throws<ArgumentNullException>(() => editContext.AddDataAnnotationsValidation());
            Assert.Equal("editContext", ex.ParamName);
        }

        [Fact]
        public void ReturnsEditContextForChaining()
        {
            var editContext = new EditContext(new object());
            var returnValue = editContext.AddDataAnnotationsValidation();
            Assert.Same(editContext, returnValue);
        }

        [Fact]
        public void GetsValidationMessagesFromDataAnnotations()
        {
            // Arrange
            var model = new TestModel { IntFrom1To100 = 101 };
            var editContext = new EditContext(model).AddDataAnnotationsValidation();

            // Act
            var isValid = editContext.Validate();

            // Assert
            Assert.False(isValid);

            Assert.Equal(new string[]
                {
                    "The RequiredString field is required.",
                    "The field IntFrom1To100 must be between 1 and 100."
                },
                editContext.GetValidationMessages());

            Assert.Equal(new string[] { "The RequiredString field is required." },
                editContext.GetValidationMessages(editContext.Field(nameof(TestModel.RequiredString))));

            // This shows we're including non-[Required] properties in the validation results, i.e,
            // that we're correctly passing "validateAllProperties: true" to DataAnnotations
            Assert.Equal(new string[] { "The field IntFrom1To100 must be between 1 and 100." },
                editContext.GetValidationMessages(editContext.Field(nameof(TestModel.IntFrom1To100))));
        }

        [Fact]
        public void ClearsExistingValidationMessagesOnFurtherRuns()
        {
            // Arrange
            var model = new TestModel { IntFrom1To100 = 101 };
            var editContext = new EditContext(model).AddDataAnnotationsValidation();

            // Act/Assert 1: Initially invalid
            Assert.False(editContext.Validate());

            // Act/Assert 2: Can become valid
            model.RequiredString = "Hello";
            model.IntFrom1To100 = 100;
            Assert.True(editContext.Validate());
        }

        class TestModel
        {
            [Required] public string RequiredString { get; set; }

            [Range(1, 100)] public int IntFrom1To100 { get; set; }
        }
    }
}
