// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

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
