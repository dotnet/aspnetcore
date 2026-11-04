// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Test;

public class EnvironmentTagHelperTest
{
    [Theory]
    [InlineData("Development", "Development")]
    [InlineData("development", "Development")]
    [InlineData("DEVELOPMENT", "Development")]
    [InlineData(" development", "Development")]
    [InlineData("development ", "Development")]
    [InlineData(" development ", "Development")]
    [InlineData("Development,Production", "Development")]
    [InlineData("Production,Development", "Development")]
    [InlineData("Development , Production", "Development")]
    [InlineData("   Development,Production   ", "Development")]
    [InlineData("Development ,  Production", "Development")]
    [InlineData("Development\t,Production", "Development")]
    [InlineData("Development,\tProduction", "Development")]
    [InlineData(" Development,Production ", "Development")]
    [InlineData("Development,Staging,Production", "Development")]
    [InlineData("Staging,Development,Production", "Development")]
    [InlineData("Staging,Production,Development", "Development")]
    [InlineData("Test", "Test")]
    [InlineData("Test,Staging", "Test")]
    public void ShowsContentWhenCurrentEnvironmentIsSpecified(string namesAttribute, string environmentName)
    {
        ShouldShowContent(namesAttribute, environmentName);
    }

    [Theory]
    [InlineData("", "Development")]
    [InlineData(null, "Development")]
    [InlineData("  ", "Development")]
    [InlineData(", ", "Development")]
    [InlineData("   , ", "Development")]
    [InlineData("\t,\t", "Development")]
    [InlineData(",", "Development")]
    [InlineData(",,", "Development")]
    [InlineData(",,,", "Development")]
    [InlineData(",,, ", "Development")]
    public void ShowsContentWhenNoEnvironmentIsSpecified(string namesAttribute, string environmentName)
    {
        ShouldShowContent(namesAttribute, environmentName);
    }

    [Theory]
    [InlineData("Development", null)]
    [InlineData("Development", "")]
    [InlineData("Development", " ")]
    [InlineData("Development", "  ")]
    [InlineData("Development", "\t")]
    [InlineData("Test", null)]
    public void ShowsContentWhenCurrentEnvironmentIsNotSet(string namesAttribute, string environmentName)
    {
        ShouldShowContent(namesAttribute, environmentName);
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("", null, "Test")]
    [InlineData("Development", "", "")]
    [InlineData("", "Development, Test", "")]
    [InlineData(null, "development, TEST", "Test")]
    [InlineData("Development", "", "Test")]
    [InlineData("Development", "Test, Development", "")]
    [InlineData("Test", "DEVELOPMENT", null)]
    [InlineData("Development", "Test", "")]
    [InlineData("Development", null, "Test")]
    [InlineData("Development", "Test", "Test")]
    [InlineData("Test", "Development", "Test")]
    public void ShouldShowContent_IncludeExcludeSpecified(string namesAttribute, string includeAttribute, string excludeAttribute)
    {
        // Arrange
        var content = "content";
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList {
                    { "names", namesAttribute },
                    { "include", includeAttribute },
                    { "exclude", excludeAttribute },
            });
        var output = MakeTagHelperOutput("environment", childContent: content);
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupProperty(h => h.EnvironmentName, "Development");

        // Act
        var helper = new EnvironmentTagHelper(hostingEnvironment.Object)
        {
            Names = namesAttribute,
            Include = includeAttribute,
            Exclude = excludeAttribute,
        };
        helper.Process(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.False(output.IsContentModified);
    }

    [Theory]
    [InlineData(null, "", "Development")]
    [InlineData("", "Development", "development")]
    [InlineData("", "Test", "Development, test")]
    [InlineData("Development", "", "Development")]
    [InlineData("Test", "", "Development")]
    [InlineData("Development", "Development", "DEVELOPMENT, TEST")]
    [InlineData("Development", "Test", "Development")]
    [InlineData("Test", "Development", "Development")]
    [InlineData("Test", "Test", "Development")]
    [InlineData("", "Test", "Test")]
    [InlineData("Test", null, "Test")]
    [InlineData("Test", "Test", "Test")]
    [InlineData("", "Test", null)]
    [InlineData("Test", "", "")]
    [InlineData("Test", "Test", null)]
    public void DoesNotShowContent_IncludeExcludeSpecified(string namesAttribute, string includeAttribute, string excludeAttribute)
    {
        // Arrange
        var content = "content";
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList {
                    { "names", namesAttribute },
                    { "include", includeAttribute },
                    { "exclude", excludeAttribute },
            });
        var output = MakeTagHelperOutput("environment", childContent: content);
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupProperty(h => h.EnvironmentName, "Development");

        // Act
        var helper = new EnvironmentTagHelper(hostingEnvironment.Object)
        {
            Names = namesAttribute,
            Include = includeAttribute,
            Exclude = excludeAttribute,
        };
        helper.Process(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.PreContent.GetContent());
        Assert.True(output.Content.GetContent().Length == 0);
        Assert.Empty(output.PostContent.GetContent());
        Assert.True(output.IsContentModified);
    }

    [Theory]
    [InlineData("NotDevelopment", "Development")]
    [InlineData("NOTDEVELOPMENT", "Development")]
    [InlineData("NotDevelopment,AlsoNotDevelopment", "Development")]
    [InlineData("Doesn'tMatchAtAll", "Development")]
    [InlineData("Development and a space", "Development")]
    [InlineData("Development and a space,SomethingElse", "Development")]
    public void DoesNotShowContentWhenCurrentEnvironmentIsNotSpecified(
        string namesAttribute,
        string environmentName)
    {
        // Arrange
        var content = "content";
        var context = MakeTagHelperContext(attributes: new TagHelperAttributeList { { "names", namesAttribute } });
        var output = MakeTagHelperOutput("environment", childContent: content);
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupProperty(h => h.EnvironmentName, environmentName);

        // Act
        var helper = new EnvironmentTagHelper(hostingEnvironment.Object)
        {
            Names = namesAttribute
        };
        helper.Process(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.PreContent.GetContent());
        Assert.True(output.Content.GetContent().Length == 0);
        Assert.Empty(output.PostContent.GetContent());
        Assert.True(output.IsContentModified);
    }

    private void ShouldShowContent(string namesAttribute, string environmentName)
    {
        // Arrange
        var content = "content";
        var context = MakeTagHelperContext(
            attributes: new TagHelperAttributeList { { "names", namesAttribute } });
        var output = MakeTagHelperOutput("environment", childContent: content);
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.SetupProperty(h => h.EnvironmentName, environmentName);

        // Act
        var helper = new EnvironmentTagHelper(hostingEnvironment.Object)
        {
            Names = namesAttribute
        };
        helper.Process(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.False(output.IsContentModified);
    }

    private TagHelperContext MakeTagHelperContext(TagHelperAttributeList attributes = null)
    {
        attributes = attributes ?? new TagHelperAttributeList();

        return new TagHelperContext(
            tagName: "env",
            allAttributes: attributes,
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString("N"));
    }

    private TagHelperOutput MakeTagHelperOutput(
        string tagName,
        TagHelperAttributeList attributes = null,
        string childContent = null)
    {
        attributes = attributes ?? new TagHelperAttributeList();

        return new TagHelperOutput(
            tagName,
            attributes,
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent(childContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
    }
}
