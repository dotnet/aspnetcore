// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Core;

public class HtmlHelperListBoxExtensionsTest
{
    private static readonly List<SelectListItem> BasicSelectList = new List<SelectListItem>
        {
            new SelectListItem("Zero", "0"),
            new SelectListItem("One", "1"),
            new SelectListItem("Two", "2"),
            new SelectListItem("Three", "3"),
        };

    [Fact]
    public void ListBox_FindsSelectList()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property1]]\" multiple=\"HtmlEncode[[multiple]]\" name=\"HtmlEncode[[Property1]]\">" +
            "<option value=\"HtmlEncode[[0]]\">HtmlEncode[[Zero]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[1]]\">HtmlEncode[[One]]</option>" + Environment.NewLine +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[2]]\">HtmlEncode[[Two]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[3]]\">HtmlEncode[[Three]]</option>" + Environment.NewLine +
            "</select>";
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.ModelState.SetModelValue("Property1", 2, "2");
        helper.ViewData["Property1"] = BasicSelectList;

        // Act
        var listBoxResult = helper.ListBox("Property1");

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(listBoxResult));
    }

    [Fact]
    public void ListBox_UsesSpecifiedExpressionAndSelectList()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property3]]\" multiple=\"HtmlEncode[[multiple]]\" name=\"HtmlEncode[[Property3]]\">" +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[4]]\">HtmlEncode[[Four]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[5]]\">HtmlEncode[[Five]]</option>" + Environment.NewLine +
            "</select>";
        var selectList = new List<SelectListItem>
            {
                new SelectListItem("Four", "4"),
                new SelectListItem("Five", "5"),
            };
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property3 = new List<string> { "4" } };

        // Act
        var listBoxResult = helper.ListBox("Property3", selectList);

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(listBoxResult));
    }

    [Fact]
    public void ListBox_UsesSpecifiedSelectExpressionAndListAndHtmlAttributes()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property2]]\" Key=\"HtmlEncode[[Value]]\" multiple=\"HtmlEncode[[multiple]]\" name=\"HtmlEncode[[Property2]]\">" +
            "<option value=\"HtmlEncode[[4]]\">HtmlEncode[[Four]]</option>" + Environment.NewLine +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[5]]\">HtmlEncode[[Five]]</option>" + Environment.NewLine +
            "</select>";
        var selectList = new List<SelectListItem>
            {
                new SelectListItem("Four", "4"),
                new SelectListItem("Five", "5"),
            };
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData["Property2"] = new List<string> { "1", "2", "5" };

        // Act
        var listBoxResult = helper.ListBox("Property2", selectList, new { Key = "Value", name = "CustomName" });

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(listBoxResult));
    }

    [Fact]
    public void ListBoxFor_NullSelectListFindsListFromViewData()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property1]]\" multiple=\"HtmlEncode[[multiple]]\" name=\"HtmlEncode[[Property1]]\">" +
            "<option value=\"HtmlEncode[[0]]\">HtmlEncode[[Zero]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[1]]\">HtmlEncode[[One]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[2]]\">HtmlEncode[[Two]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[3]]\">HtmlEncode[[Three]]</option>" + Environment.NewLine +
            "</select>";
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData["Property1"] = BasicSelectList;

        // Act
        var listBoxForResult = helper.ListBoxFor(m => m.Property1, null);

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(listBoxForResult));
    }

    [Fact]
    public void ListBoxFor_UsesSpecifiedExpressionAndSelectList()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property3]]\" multiple=\"HtmlEncode[[multiple]]\" name=\"HtmlEncode[[Property3]]\">" +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[4]]\">HtmlEncode[[Four]]</option>" + Environment.NewLine +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[5]]\">HtmlEncode[[Five]]</option>" + Environment.NewLine +
            "</select>";
        var selectList = new List<SelectListItem>
            {
                new SelectListItem("Four", "4"),
                new SelectListItem("Five", "5"),
            };
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.Model = new TestModel { Property3 = new List<string> { "0", "4", "5" } };

        // Act
        var listBoxForResult = helper.ListBoxFor(m => m.Property3, selectList);

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(listBoxForResult));
    }

    [Fact]
    public void ListBoxFor_UsesSpecifiedExpressionAndSelectListAndHtmlAttributes()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property3]]\" Key=\"HtmlEncode[[Value]]\" multiple=\"HtmlEncode[[multiple]]\" name=\"HtmlEncode[[Property3]]\">" +
            "<option value=\"HtmlEncode[[4]]\">HtmlEncode[[Four]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[5]]\">HtmlEncode[[Five]]</option>" + Environment.NewLine +
            "</select>";
        var selectList = new List<SelectListItem>
            {
                new SelectListItem("Four", "4"),
                new SelectListItem("Five", "5"),
            };
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData["Property3"] = new List<string> { "0", "2" };

        // Act
        var listBoxForResult = helper.ListBoxFor(m => m.Property3, selectList, new { Key = "Value", name = "CustomName" });

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(listBoxForResult));
    }

    private class TestModel
    {
        public int Property1 { get; set; }

        public string Property2 { get; set; }

        public List<string> Property3 { get; set; }
    }
}
