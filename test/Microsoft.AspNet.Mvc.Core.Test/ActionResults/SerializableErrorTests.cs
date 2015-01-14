// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Xml;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class SerializableErrorTests
    {
        [Fact]
        public void ConvertsModelState_To_Dictionary()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("key1", "Test Error 1");
            modelState.AddModelError("key1", "Test Error 2");
            modelState.AddModelError("key2", "Test Error 3");

            // Act
            var serializableError = new SerializableError(modelState);

            // Assert
            var arr = Assert.IsType<string[]>(serializableError["key1"]);
            Assert.Equal("Test Error 1", arr[0]);
            Assert.Equal("Test Error 2", arr[1]);
            Assert.Equal("Test Error 3", (serializableError["key2"] as string[])[0]);
        }

        [Fact]
        public void LookupIsCaseInsensitive()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("key1", "x");

            // Act
            var serializableError = new SerializableError(modelState);

            // Assert
            var arr = Assert.IsType<string[]>(serializableError["KEY1"]);
            Assert.Equal("x", arr[0]);
        }

        [Fact]
        public void ConvertsModelState_To_Dictionary_AddsDefaultValuesWhenErrorsAreAbsent()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("key1", "");

            // Act
            var serializableError = new SerializableError(modelState);

            // Assert
            var arr = Assert.IsType<string[]>(serializableError["key1"]);
            Assert.Equal("The input was not valid.", arr[0]);
        }

        [Fact]
        public void DoesNotThrowOnValidModelState()
        {
            // Arrange, Act & Assert (does not throw)
            new SerializableError(new ModelStateDictionary());
        }

        [Fact]
        public void DoesNotAddEntries_IfNoErrorsArePresent()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.Add(
                "key1",
                new ModelState() { Value = new ValueProviderResult("foo", "foo", CultureInfo.InvariantCulture) });
            modelState.Add(
                "key2",
                new ModelState() { Value = new ValueProviderResult("bar", "bar", CultureInfo.InvariantCulture) });

            // Act
            var serializableError = new SerializableError(modelState);

            // Assert
            Assert.Equal(0, serializableError.Count);
        }
    }
}