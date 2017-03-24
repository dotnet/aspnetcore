// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class TagHelperRequiredAttributeDescriptorTest
    {
        public static TheoryData RequiredAttributeDescriptorData
        {
            get
            {
                // requiredAttributeDescriptor, attributeName, attributeValue, expectedResult
                return new TheoryData<RequiredAttributeDescriptor, string, string, bool>
                {
                    {
                        RequiredAttributeDescriptorBuilder.Create().Name("key").Build(),
                        "KeY",
                        "value",
                        true
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create().Name("key").Build(),
                        "keys",
                        "value",
                        false
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("route-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)
                            .Build(),
                        "ROUTE-area",
                        "manage",
                        true
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("route-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)
                            .Build(),
                        "routearea",
                        "manage",
                        false
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("route-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)
                            .Build(),
                        "route-",
                        "manage",
                        false
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Build(),
                        "KeY",
                        "value",
                        true
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Build(),
                        "keys",
                        "value",
                        false
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("value")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch)
                            .Build(),
                        "key",
                        "value",
                        true
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("value")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch)
                            .Build(),
                        "key",
                        "Value",
                        false
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("class")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("btn")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch)
                            .Build(),
                        "class",
                        "btn btn-success",
                        true
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("class")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("btn")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch)
                            .Build(),
                        "class",
                        "BTN btn-success",
                        false
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("href")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("#navigate")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch)
                            .Build(),
                        "href",
                        "/home/index#navigate",
                        true
                    },
                    {
                        RequiredAttributeDescriptorBuilder.Create()
                            .Name("href")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("#navigate")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch)
                            .Build(),
                        "href",
                        "/home/index#NAVigate",
                        false
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredAttributeDescriptorData))]
        public void Matches_ReturnsExpectedResult(
            object requiredAttributeDescriptor,
            string attributeName,
            string attributeValue,
            bool expectedResult)
        {
            // Act
            var result = ((RequiredAttributeDescriptor)requiredAttributeDescriptor).IsMatch(attributeName, attributeValue);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
