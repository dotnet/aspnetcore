// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TagHelperMatchingConventionsTest
    {
        public static TheoryData RequiredAttributeDescriptorData
        {
            get
            {
                // requiredAttributeDescriptor, attributeName, attributeValue, expectedResult
                return new TheoryData<Action<RequiredAttributeDescriptorBuilder>, string, string, bool>
                {
                    {
                        builder => builder.Name("key"),
                        "KeY",
                        "value",
                        true
                    },
                    {
                        builder => builder.Name("key"),
                        "keys",
                        "value",
                        false
                    },
                    {
                        builder => builder
                            .Name("route-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch),
                        "ROUTE-area",
                        "manage",
                        true
                    },
                    {
                        builder => builder
                            .Name("route-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch),
                        "routearea",
                        "manage",
                        false
                    },
                    {
                        builder => builder
                            .Name("route-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch),
                        "route-",
                        "manage",
                        false
                    },
                    {
                        builder => builder
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch),
                        "KeY",
                        "value",
                        true
                    },
                    {
                        builder => builder
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch),
                        "keys",
                        "value",
                        false
                    },
                    {
                        builder => builder
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("value")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch),
                        "key",
                        "value",
                        true
                    },
                    {
                        builder => builder
                            .Name("key")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("value")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch),
                        "key",
                        "Value",
                        false
                    },
                    {
                        builder => builder
                            .Name("class")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("btn")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch),
                        "class",
                        "btn btn-success",
                        true
                    },
                    {
                        builder => builder
                            .Name("class")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("btn")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch),
                        "class",
                        "BTN btn-success",
                        false
                    },
                    {
                        builder => builder
                            .Name("href")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("#navigate")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch),
                        "href",
                        "/home/index#navigate",
                        true
                    },
                    {
                        builder => builder
                            .Name("href")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("#navigate")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch),
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
            Action<RequiredAttributeDescriptorBuilder> configure,
            string attributeName,
            string attributeValue,
            bool expectedResult)
        {
            // Arrange

            var builder = new DefaultRequiredAttributeDescriptorBuilder();
            configure(builder);

            var requiredAttibute = builder.Build();

            // Act
            var result = TagHelperMatchingConventions.SatisfiesRequiredAttribute(attributeName, attributeValue, requiredAttibute);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
