// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperDescriptorComparerTest
{
    private static readonly TestFile TagHelpersTestFile = TestFile.Create("TestFiles/taghelpers.json", typeof(TagHelperDescriptorComparerTest));

    [Fact]
    public void GetHashCode_SameTagHelperDescriptors_HashCodeMatches()
    {
        // Arrange
        var descriptor1 = CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                });
        var descriptor2 = CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                });

        // Act
        var hashCode1 = descriptor1.GetHashCode();
        var hashCode2 = descriptor2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_FQNAndNameTagHelperDescriptors_HashCodeDoesNotMatch()
    {
        // Arrange
        var descriptorName = CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                tagMatchingRuleName: "Input",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                });
        var descriptorFQN = CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                tagMatchingRuleName: "Microsoft.AspNetCore.Components.Forms.Input",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                });

        // Act
        var hashCodeName = descriptorName.GetHashCode();
        var hashCodeFQN = descriptorFQN.GetHashCode();

        // Assert
        Assert.NotEqual(hashCodeName, hashCodeFQN);
    }

    [Fact]
    public void GetHashCode_DifferentTagHelperDescriptors_HashCodeDoesNotMatch()
    {
        // Arrange
        var counterTagHelper = CreateTagHelperDescriptor(
                tagName: "Counter",
                typeName: "CounterTagHelper",
                assemblyName: "Components.Component",
                tagMatchingRuleName: "Input",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                        builder => builder
                            .Name("IncrementBy")
                            .PropertyName("IncrementBy")
                            .TypeName("System.Int32"),
                });
        var inputTagHelper = CreateTagHelperDescriptor(
                tagName: "input",
                typeName: "InputTagHelper",
                assemblyName: "TestAssembly",
                tagMatchingRuleName: "Microsoft.AspNetCore.Components.Forms.Input",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                });

        // Act
        var hashCodeCounter = counterTagHelper.GetHashCode();
        var hashCodeInput = inputTagHelper.GetHashCode();

        // Assert
        Assert.NotEqual(hashCodeCounter, hashCodeInput);
    }

    [Fact]
    public void GetHashCode_AllTagHelpers_NoHashCodeCollisions()
    {
        // Arrange
        var tagHelpers = ReadTagHelpers(TagHelpersTestFile.OpenRead());

        // Act
        var hashes = new HashSet<int>(tagHelpers.Select(t => t.GetHashCode()));

        // Assert
        Assert.Equal(hashes.Count, tagHelpers.Count);
    }

    [Fact]
    public void GetHashCode_DuplicateTagHelpers_NoHashCodeCollisions()
    {
        // Arrange
        var tagHelpers = new List<TagHelperDescriptor>();
        var tagHelpersPerBatch = -1;

        // Reads 5 copies of the TagHelpers (with 5x references)
        // This ensures we don't have any dependencies on reference based GetHashCode
        for (var i = 0; i < 5; ++i)
        {
            var tagHelpersBatch = ReadTagHelpers(TagHelpersTestFile.OpenRead());
            tagHelpers.AddRange(tagHelpersBatch);
            tagHelpersPerBatch = tagHelpersBatch.Count;
        }

        // Act
        var hashes = new HashSet<int>(tagHelpers.Select(t => t.GetHashCode()));

        // Assert
        // Only 1 batch of taghelpers should remain after we filter by hash
        Assert.Equal(hashes.Count, tagHelpersPerBatch);
    }

    private static TagHelperDescriptor CreateTagHelperDescriptor(
        string tagName,
        string typeName,
        string assemblyName,
        string tagMatchingRuleName = null,
        IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
    {
        var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName) as DefaultTagHelperDescriptorBuilder;
        builder.TypeName(typeName);

        if (attributes != null)
        {
            foreach (var attributeBuilder in attributes)
            {
                builder.BoundAttributeDescriptor(attributeBuilder);
            }
        }

        builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagMatchingRuleName ?? tagName));

        var descriptor = builder.Build();

        return descriptor;
    }

    private IReadOnlyList<TagHelperDescriptor> ReadTagHelpers(Stream stream)
    {
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new RazorDiagnosticJsonConverter());
        serializer.Converters.Add(new TagHelperDescriptorJsonConverter());

        IReadOnlyList<TagHelperDescriptor> result;

        using var streamReader = new StreamReader(stream);
        using (var reader = new JsonTextReader(streamReader))
        {
            result = serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
        }

        stream.Dispose();

        return result;
    }
}
