// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Core;

public class HtmlHelperDropDownListExtensionsTest
{
    private static readonly List<SelectListItem> BasicSelectList = new List<SelectListItem>
        {
            new SelectListItem("Zero", "0"),
            new SelectListItem("One", "1"),
            new SelectListItem("Two", "2"),
            new SelectListItem("Three", "3"),
        };

    [Fact]
    public void DropDownList_FindsSelectList()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" +
            "<option value=\"HtmlEncode[[0]]\">HtmlEncode[[Zero]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[1]]\">HtmlEncode[[One]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[2]]\">HtmlEncode[[Two]]</option>" + Environment.NewLine +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[3]]\">HtmlEncode[[Three]]</option>" + Environment.NewLine +
            "</select>";
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.ModelState.SetModelValue("Property1", 3, "3");
        helper.ViewData["Property1"] = BasicSelectList;

        // Act
        var dropDownListResult = helper.DropDownList("Property1");

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListResult));
    }

    [Fact]
    public void DropDownList_FindsSelectList_UsesOptionLabel()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" +
            "<option value=\"\">HtmlEncode[[--select--]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[0]]\">HtmlEncode[[Zero]]</option>" + Environment.NewLine +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[1]]\">HtmlEncode[[One]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[2]]\">HtmlEncode[[Two]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[3]]\">HtmlEncode[[Three]]</option>" + Environment.NewLine +
            "</select>";
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData.ModelState.SetModelValue("Property1", 1, "1");
        helper.ViewData["Property1"] = BasicSelectList;

        // Act
        var dropDownListResult = helper.DropDownList("Property1", "--select--");

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListResult));
    }

    [Fact]
    public void DropDownList_UsesSpecifiedExpressionAndSelectList()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property2]]\" name=\"HtmlEncode[[Property2]]\">" +
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
        helper.ViewData.Model = new TestModel { Property2 = "4" };

        // Act
        var dropDownListResult = helper.DropDownList("Property2", selectList);

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListResult));
    }

    [Fact]
    public void DropDownList_UsesSpecifiedExpressionAndSelectListAndHtmlAttributes()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property1]]\" Key=\"HtmlEncode[[Value]]\" name=\"HtmlEncode[[Property1]]\">" +
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
        helper.ViewData.ModelState.SetModelValue("Property1", 4, "4");
        helper.ViewData["Property1"] = BasicSelectList;

        // Act
        var dropDownListResult = helper.DropDownList("Property1", selectList, new { Key = "Value", name = "CustomName" });

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListResult));
    }

    [Fact]
    public void DropDownList_UsesExpressionAndSpecifiedSelectListAndOptionLabel()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property2]]\" name=\"HtmlEncode[[Property2]]\">" +
            "<option value=\"\">HtmlEncode[[--select--]]</option>" + Environment.NewLine +
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
        helper.ViewData.Model = new TestModel { Property2 = "5" };

        // Act
        var dropDownListResult = helper.DropDownList("Property2", selectList, "--select--");

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListResult));
    }

    [Fact]
    public void DropDownListFor_NullSelectListFindsListFromViewData()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property1]]\" name=\"HtmlEncode[[Property1]]\">" +
            "<option selected=\"HtmlEncode[[selected]]\" value=\"HtmlEncode[[0]]\">HtmlEncode[[Zero]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[1]]\">HtmlEncode[[One]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[2]]\">HtmlEncode[[Two]]</option>" + Environment.NewLine +
            "<option value=\"HtmlEncode[[3]]\">HtmlEncode[[Three]]</option>" + Environment.NewLine +
            "</select>";
        var metadataProvider = new EmptyModelMetadataProvider();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(new ViewDataDictionary<TestModel>(metadataProvider));
        helper.ViewContext.ClientValidationEnabled = false;
        helper.ViewData["Property1"] = BasicSelectList;
        helper.ViewData.Model = new TestModel { Property1 = 0 };

        // Act
        var dropDownListForResult = helper.DropDownListFor(m => m.Property1, selectList: null);

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListForResult));
    }

    [Fact]
    public void DropDownListFor_UsesSpecifiedExpressionAndSelectList()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property2]]\" name=\"HtmlEncode[[Property2]]\">" +
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
        helper.ViewData["Property1"] = BasicSelectList;
        helper.ViewData.Model = new TestModel { Property2 = "5" };

        // Act
        var dropDownListForResult = helper.DropDownListFor(m => m.Property2, selectList);

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListForResult));
    }

    [Fact]
    public void DropDownListFor_UsesSpecifiedExpressionAndSelectListAndHtmlAttributes()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property3_2_]]\" Key=\"HtmlEncode[[Value]]\" name=\"HtmlEncode[[Property3[2]]]\">" +
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
        helper.ViewData.Model = new TestModel { Property3 = new List<string> { "0", "2", "4" } };

        // Act
        var dropDownListForResult = helper.DropDownListFor(m => m.Property3[2], selectList, new { Key = "Value", name = "CustomName" });

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListForResult));
    }

    [Fact]
    public void DropDownListFor_UsesSpecifiedSelectListAndOptionLabel()
    {
        // Arrange
        var expectedHtml = "<select id=\"HtmlEncode[[Property2]]\" name=\"HtmlEncode[[Property2]]\">" +
            "<option value=\"\">HtmlEncode[[--select--]]</option>" + Environment.NewLine +
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
        helper.ViewData.Model = new TestModel { Property2 = "5" };

        // Act
        var dropDownListForResult = helper.DropDownListFor(m => m.Property2, selectList, "--select--");

        // Assert
        Assert.Equal(expectedHtml, HtmlContentUtilities.HtmlContentToString(dropDownListForResult));
    }

    private class TestModel
    {
        public int Property1 { get; set; }

        public string Property2 { get; set; }

        public List<string> Property3 { get; set; }
    }
}
