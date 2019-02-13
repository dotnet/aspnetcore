// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class ModelExplorerExtensionsTest
    {
        public static TheoryData<object, Type, string> SimpleDisplayTextData
        {
            get
            {
                return new TheoryData<object, Type, string>
                {
                    {
                        new ComplexClass()
                        {
                            Prop1 = new Class1 { Prop1 = "Hello" }
                        },
                        typeof(ComplexClass),
                        "Class1"
                    },
                    {
                        new Class1(),
                        typeof(Class1),
                        "Class1"
                    },
                    {
                        new ClassWithNoProperties(),
                        typeof(ClassWithNoProperties),
                        string.Empty
                    },
                    {
                        null,
                        typeof(object),
                        null
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SimpleDisplayTextData))]
        public void GetSimpleDisplayText_WithoutSimpleDisplayProperty(
            object model, 
            Type modelType, 
            string expectedResult)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var modelExplorer = provider.GetModelExplorerForType(modelType, model);

            // Act
            var result = modelExplorer.GetSimpleDisplayText();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        private class ClassWithNoProperties
        {
            public override string ToString()
            {
                return null;
            }
        }

        private class ComplexClass
        {
            public Class1 Prop1 { get; set; }
        }

        private class Class1
        {
            public string Prop1 { get; set; }

            public override string ToString()
            {
                return "Class1";
            }
        }
    }
}