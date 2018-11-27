// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class HtmlHelperEditorExtensionsTest
    {
        [Theory]
        [MemberData(nameof(HtmlHelperDisplayExtensionsTest.EnumUnformattedModels),
            MemberType = typeof(HtmlHelperDisplayExtensionsTest))]
        public void Display_UsesTemplateUnFormatted(HtmlHelperDisplayExtensionsTest.FormatModel model, string expectedResult)
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
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), "EditorTemplates/Status", /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found("Status", view.Object))
                .Verifiable();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var displayResult = helper.EditorFor(x => x.NonFormatProperty);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(displayResult));
        }

        [Theory]
        [MemberData(nameof(HtmlHelperDisplayExtensionsTest.EnumFormatModels), MemberType = typeof(HtmlHelperDisplayExtensionsTest))]
        public void Display_UsesTemplateFormatted(HtmlHelperDisplayExtensionsTest.FormatModel model, string expectedResult)
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
                .Setup(v => v.FindView(It.IsAny<ActionContext>(), "EditorTemplates/Status", /*isMainPage*/ false))
                .Returns(ViewEngineResult.Found("Status", view.Object))
                .Verifiable();
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);

            // Act
            var displayResult = helper.EditorFor(x => x.FormatProperty);

            // Assert
            Assert.Equal(expectedResult, HtmlContentUtilities.HtmlContentToString(displayResult));
        }
    }
}
