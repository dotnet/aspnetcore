// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ValidationProblemDescriptionTest
    {
        [Fact]
        public void Constructor_SetsTitle()
        {
            // Arrange & Act
            var problemDescription = new ValidationProblemDescription();

            // Assert
            Assert.Equal("One or more validation errors occured.", problemDescription.Title);
            Assert.Empty(problemDescription.Errors);
        }

        [Fact]
        public void Constructor_SerializesErrorsFromModelStateDictionary()
        {
            // Arrange
            var modelStateDictionary = new ModelStateDictionary();
            modelStateDictionary.AddModelError("key1", "error1");
            modelStateDictionary.SetModelValue("key2", new object(), "value");
            modelStateDictionary.AddModelError("key3", "error2");
            modelStateDictionary.AddModelError("key3", "error3");

            // Act
            var problemDescription = new ValidationProblemDescription(modelStateDictionary);

            // Assert
            Assert.Equal("One or more validation errors occured.", problemDescription.Title);
            Assert.Collection(
                problemDescription.Errors,
                item =>
                {
                    Assert.Equal("key1", item.Key);
                    Assert.Equal(new[] { "error1" }, item.Value);
                },
                item =>
                {
                    Assert.Equal("key3", item.Key);
                    Assert.Equal(new[] { "error2", "error3" }, item.Value);
                });
        }
    }
}
