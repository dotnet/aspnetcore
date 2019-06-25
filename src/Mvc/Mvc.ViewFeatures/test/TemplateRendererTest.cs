// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class TemplateRendererTest
    {
        public static TheoryData<Type, string[]> TypeNameData
        {
            get
            {
                return new TheoryData<Type, string[]>
                {
                    { typeof(string), new string[] { "String" } },
                    { typeof(bool), new string[] { "Boolean", "String" } },
                    { typeof(DateTime), new string[] { "DateTime", "String" } },
                    { typeof(float), new string[] { "Single", "String" } },
                    { typeof(double), new string[] { "Double", "String" } },
                    { typeof(Guid), new string[] { "Guid", "String" } },
                    { typeof(TimeSpan), new string[] { "TimeSpan", "String" } },
                    { typeof(int), new string[] { "Int32", "String" } },
                    { typeof(ulong), new string[] { "UInt64", "String" } },

                    { typeof(Enum), new string[] { "Enum", "String" } },
                    { typeof(HttpStatusCode), new string[] { "HttpStatusCode", "Enum", "String" } },

                    { typeof(FormFile), new string[] { "FormFile", "IFormFile", "Object" } },
                    { typeof(IFormFile), new string[] { "IFormFile", "Object" } },

                    { typeof(FormFileCollection), new string[] { "FormFileCollection", typeof(List<IFormFile>).Name,
                        TemplateRenderer.IEnumerableOfIFormFileName, "Collection", "Object" } },
                    { typeof(IFormFileCollection), new string[] { "IFormFileCollection",
                        TemplateRenderer.IEnumerableOfIFormFileName, "Collection", "Object" } },
                    { typeof(IEnumerable<IFormFile>), new string[] { TemplateRenderer.IEnumerableOfIFormFileName,
                        typeof(IEnumerable<IFormFile>).Name, "Collection", "Object" } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TypeNameData))]
        public void GetTypeNames_ReturnsExpectedResults(Type fieldType, string[] expectedResult)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var metadata = metadataProvider.GetMetadataForType(fieldType);

            // Act
            var typeNames = TemplateRenderer.GetTypeNames(metadata, fieldType);

            // Assert
            var collectionAssertions = expectedResult.Select<string, Action<string>>(expected =>
                actual => Assert.Equal(expected, actual));
            Assert.Collection(typeNames, collectionAssertions.ToArray());
        }
    }
}