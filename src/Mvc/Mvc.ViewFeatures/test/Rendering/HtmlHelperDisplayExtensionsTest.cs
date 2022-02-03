// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Localization;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperDisplayExtensionsTest
{
    [Fact]
    public void DisplayHelpers_FindsModel_WhenViewDataIsNotSet()
    {
        // Arrange
        var expected = $"<div class=\"HtmlEncode[[display-label]]\">HtmlEncode[[SomeProperty]]</div>{Environment.NewLine}" +
            $"<div class=\"HtmlEncode[[display-field]]\">HtmlEncode[[PropValue]]</div>{Environment.NewLine}";
        var model = new SomeModel
        {
            SomeProperty = "PropValue"
        };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.Display(expression: string.Empty);
        var displayNullResult = helper.Display(expression: null);   // null is another alias for current model
        var displayForResult = helper.DisplayFor(m => m);
        var displayForModelResult = helper.DisplayForModel();

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(displayResult));
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(displayNullResult));
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(displayForResult));
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(displayForModelResult));
    }

    [Fact]
    public void Display_UsesAdditionalViewData()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData["SomeProperty"].ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.Display(expression: "SomeProperty", additionalViewData: new { SomeProperty = "ViewDataValue" });

        // Assert
        Assert.Equal("ViewDataValue", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void Display_UsesTemplateName()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", Mock.Of<IView>()))
            .Verifiable();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.Display(expression: "SomeProperty", templateName: "SomeTemplate");

        // Assert
        viewEngine.Verify();
    }

    public static TheoryData<FormatModel, string> EnumFormatModels
    {
        get
        {
            return new TheoryData<FormatModel, string>
                {
                    {
                        new FormatModel{ FormatProperty = Status.Created },
                        "Value: Created"
                    },
                    {
                        new FormatModel { FormatProperty = Status.Done },
                        "Value: Done"
                    }
                };
        }
    }

    public static TheoryData<FormatModel, string> EnumUnformattedModels
    {
        get
        {
            return new TheoryData<FormatModel, string>
                {
                    {
                        new FormatModel {NonFormatProperty = Status.Created },
                        "CreatedKey"
                    },
                    {
                        new FormatModel {NonFormatProperty = Status.Done },
                        "Done"
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(EnumUnformattedModels))]
    public void Display_UsesTemplateUnFormatted(FormatModel model, string expectedResult)
    {
        // Arrange
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.FormattedModelValue.ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/Status", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Status", view.Object))
            .Verifiable();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(x => x.NonFormatProperty);

        // Assert
        Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Theory]
    [MemberData(nameof(EnumFormatModels))]
    public void Display_UsesTemplateFormatted(FormatModel model, string expectedResult)
    {
        // Arrange
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.FormattedModelValue.ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/Status", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("Status", view.Object))
            .Verifiable();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(x => x.FormatProperty);

        // Assert
        Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void Display_UsesTemplateNameAndAdditionalViewData()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData["SomeProperty"].ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.Display(
            expression: "SomeProperty",
            templateName: "SomeTemplate",
            additionalViewData: new { SomeProperty = "ViewDataValue" });

        // Assert
        Assert.Equal("ViewDataValue", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void Display_UsesTemplateNameAndHtmlFieldName()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.HtmlFieldPrefix))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.Display(
            expression: "SomeProperty",
            templateName: "SomeTemplate",
            htmlFieldName: "SomeField");

        // Assert
        Assert.Equal("SomeField", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayFor_UsesAdditionalViewData()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData["SomeProperty"].ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(expression: m => m.SomeProperty, additionalViewData: new { SomeProperty = "ViewDataValue" });

        // Assert
        Assert.Equal("ViewDataValue", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayFor_UsesTemplateName()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", Mock.Of<IView>()))
            .Verifiable();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(expression: m => m.SomeProperty, templateName: "SomeTemplate");

        // Assert
        viewEngine.Verify();
    }

    [Fact]
    public void DisplayFor_UsesTemplateNameAndAdditionalViewData()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData["SomeProperty"].ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(
            expression: m => m.SomeProperty,
            templateName: "SomeTemplate",
            additionalViewData: new { SomeProperty = "ViewDataValue" });

        // Assert
        Assert.Equal("ViewDataValue", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayFor_EnumProperty_IStringLocalizedValue()
    {
        // Arrange
        var model = new StatusModel
        {
            Status = Status.Created
        };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.FormattedModelValue.ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/Status", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));

        var stringLocalizer = new Mock<IStringLocalizer>(MockBehavior.Strict);
        stringLocalizer
            .Setup(s => s["CreatedKey"])
            .Returns<string>((key) =>
            {
                return new LocalizedString(key, "created from IStringLocalizer");
            });
        var stringLocalizerFactory = new Mock<IStringLocalizerFactory>();
        stringLocalizerFactory
            .Setup(s => s.Create(typeof(Status)))
            .Returns(stringLocalizer.Object);

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object, stringLocalizerFactory.Object);

        // Act
        var displayResult = helper.DisplayFor(m => m.Status);

        // Assert
        Assert.Equal("created from IStringLocalizer", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayFor_EnumProperty_ResourceTypeLocalizedValue()
    {
        // Arrange
        var model = new StatusModel
        {
            Status = Status.Faulted
        };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.FormattedModelValue.ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/Status", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(m => m.Status);

        // Assert
        Assert.Equal("Faulted from ResourceType", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayFor_UsesTemplateNameAndHtmlFieldName()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.HtmlFieldPrefix))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayFor(
            expression: m => m.SomeProperty,
            templateName: "SomeTemplate",
            htmlFieldName: "SomeField");

        // Assert
        Assert.Equal("SomeField", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayForModel_UsesAdditionalViewData()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData["SomeProperty"].ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayForModel(additionalViewData: new { SomeProperty = "ViewDataValue" });

        // Assert
        Assert.Equal("ViewDataValue", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayForModel_UsesTemplateName()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", Mock.Of<IView>()))
            .Verifiable();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayForModel(templateName: "SomeTemplate");

        // Assert
        viewEngine.Verify();
    }

    [Fact]
    public void DisplayForModel_UsesTemplateNameAndAdditionalViewData()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData["SomeProperty"].ToString()))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayForModel(
            templateName: "SomeTemplate",
            additionalViewData: new { SomeProperty = "ViewDataValue" });

        // Assert
        Assert.Equal("ViewDataValue", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    [Fact]
    public void DisplayForModel_UsesTemplateNameAndHtmlFieldName()
    {
        // Arrange
        var model = new SomeModel { SomeProperty = "ModelValue" };
        var view = new Mock<IView>();
        view.Setup(v => v.RenderAsync(It.IsAny<ViewContext>()))
            .Callback((ViewContext v) => v.Writer.WriteAsync(v.ViewData.TemplateInfo.HtmlFieldPrefix))
            .Returns(Task.FromResult(0));
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound(string.Empty, Enumerable.Empty<string>()));
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), "DisplayTemplates/SomeTemplate", /*isMainPage*/ false))
            .Returns(ViewEngineResult.Found("SomeView", view.Object));
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

        // Act
        var displayResult = helper.DisplayForModel(
            templateName: "SomeTemplate",
            htmlFieldName: "SomeField");

        // Assert
        Assert.Equal("SomeField", HtmlContentUtilities.HtmlContentToString(displayResult));
    }

    public class StatusResource
    {
        public static string FaultedKey { get { return "Faulted from ResourceType"; } }
    }

    public enum Status : byte
    {
        [Display(Name = "CreatedKey")]
        Created,
        [Display(Name = "FaultedKey", ResourceType = typeof(StatusResource))]
        Faulted,
        Done
    }

    public class FormatModel
    {
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "Value: {0}")]
        public Status FormatProperty { get; set; }

        public Status NonFormatProperty { get; set; }
    }

    private class SomeModel
    {
        public string SomeProperty { get; set; }
    }

    private class StatusModel
    {
        public Status Status { get; set; }
    }
}
