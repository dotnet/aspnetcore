// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
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

        private class SomeModel
        {
            public string SomeProperty { get; set; }
        }
    }
}
