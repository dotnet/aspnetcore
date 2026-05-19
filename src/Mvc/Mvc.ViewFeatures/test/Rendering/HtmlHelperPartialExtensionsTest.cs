// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlHelperPartialExtensionsTest
{
    // Func<IHtmlHelper, IHtmlContent>, expected Model, expected ViewDataDictionary
    public static TheoryData<Func<IHtmlHelper, IHtmlContent>, object, ViewDataDictionary> PartialExtensionMethods
    {
        get
        {
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var model = new object();
            return new TheoryData<Func<IHtmlHelper, IHtmlContent>, object, ViewDataDictionary>
                {
                    { helper => helper.Partial("test"), null, null },
                    { helper => helper.Partial("test", model), model, null },
                    { helper => helper.Partial("test", viewData), null, viewData },
                    { helper => helper.Partial("test", model, viewData), model, viewData },
                };
        }
    }

    [Theory]
    [MemberData(nameof(PartialExtensionMethods))]
    public void PartialMethods_CallHtmlHelperWithExpectedArguments(
        Func<IHtmlHelper, IHtmlContent> partialMethod,
        object expectedModel,
        ViewDataDictionary expectedViewData)
    {
        // Arrange
        var htmlContent = Mock.Of<IHtmlContent>();
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        if (expectedModel == null)
        {
            // Extension methods without model parameter use ViewData.Model to get Model.
            var viewData = expectedViewData ?? new ViewDataDictionary(new EmptyModelMetadataProvider());
            helper
                .SetupGet(h => h.ViewData)
                .Returns(viewData)
                .Verifiable();
        }

        helper
            .Setup(h => h.PartialAsync("test", expectedModel, expectedViewData))
            .Returns(Task.FromResult(htmlContent))
            .Verifiable();

        // Act
        var result = partialMethod(helper.Object);

        // Assert
        Assert.Same(htmlContent, result);
        helper.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(PartialExtensionMethods))]
    public void PartialMethods_DoesNotWrapThrownException(
        Func<IHtmlHelper, IHtmlContent> partialMethod,
        object unusedModel,
        ViewDataDictionary unusedViewData)
    {
        // Arrange
        var expected = new InvalidOperationException();
        var helper = new Mock<IHtmlHelper>();
        helper.Setup(h => h.PartialAsync("test", It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
              .Callback(() =>
              {
                  // Workaround for compilation issue with Moq.
                  helper.ToString();
                  throw expected;
              });
        helper.SetupGet(h => h.ViewData)
              .Returns(new ViewDataDictionary(new EmptyModelMetadataProvider()));

        // Act and Assert
        var actual = Assert.Throws<InvalidOperationException>(() => partialMethod(helper.Object));
        Assert.Same(expected, actual);
    }

    // Func<IHtmlHelper, IHtmlContent>, expected Model, expected ViewDataDictionary
    public static TheoryData<Func<IHtmlHelper, Task<IHtmlContent>>, object, ViewDataDictionary> PartialAsyncExtensionMethods
    {
        get
        {
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var model = new object();
            return new TheoryData<Func<IHtmlHelper, Task<IHtmlContent>>, object, ViewDataDictionary>
                {
                    { helper => helper.PartialAsync("test"), null, null },
                    { helper => helper.PartialAsync("test", model), model, null },
                    { helper => helper.PartialAsync("test", viewData), null, viewData },
                };
        }
    }

    [Theory]
    [MemberData(nameof(PartialAsyncExtensionMethods))]
    public async Task PartialAsyncMethods_CallHtmlHelperWithExpectedArguments(
        Func<IHtmlHelper, Task<IHtmlContent>> partialAsyncMethod,
        object expectedModel,
        ViewDataDictionary expectedViewData)
    {
        // Arrange
        var htmlContent = Mock.Of<IHtmlContent>();
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        if (expectedModel == null)
        {
            // Extension methods without model parameter use ViewData.Model to get Model.
            var viewData = expectedViewData ?? new ViewDataDictionary(new EmptyModelMetadataProvider());
            helper
                .SetupGet(h => h.ViewData)
                .Returns(viewData)
                .Verifiable();
        }

        helper
            .Setup(h => h.PartialAsync("test", expectedModel, expectedViewData))
            .Returns(Task.FromResult(htmlContent))
            .Verifiable();

        // Act
        var result = await partialAsyncMethod(helper.Object);

        // Assert
        Assert.Same(htmlContent, result);
        helper.VerifyAll();
    }

    // Func<IHtmlHelper, IHtmlContent>, expected Model, expected ViewDataDictionary
    public static TheoryData<Action<IHtmlHelper>, object, ViewDataDictionary> RenderPartialExtensionMethods
    {
        get
        {
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var model = new object();
            return new TheoryData<Action<IHtmlHelper>, object, ViewDataDictionary>
                {
                    { helper => helper.RenderPartial("test"), null, null },
                    { helper => helper.RenderPartial("test", model), model, null },
                    { helper => helper.RenderPartial("test", viewData), null, viewData },
                    { helper => helper.RenderPartial("test", model, viewData), model, viewData },
                };
        }
    }

    [Theory]
    [MemberData(nameof(RenderPartialExtensionMethods))]
    public void RenderPartialMethods_DoesNotWrapThrownException(
        Action<IHtmlHelper> partialMethod,
        object unusedModel,
        ViewDataDictionary unusedViewData)
    {
        // Arrange
        var expected = new InvalidOperationException();
        var helper = new Mock<IHtmlHelper>();
        helper.Setup(h => h.RenderPartialAsync("test", It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
              .Callback(() =>
              {
                  // Workaround for compilation issue with Moq.
                  helper.ToString();
                  throw expected;
              });
        helper.SetupGet(h => h.ViewData)
              .Returns(new ViewDataDictionary(new EmptyModelMetadataProvider()));

        // Act and Assert
        var actual = Assert.Throws<InvalidOperationException>(() => partialMethod(helper.Object));
        Assert.Same(expected, actual);
    }

    [Theory]
    [MemberData(nameof(RenderPartialAsyncExtensionMethods))]
    public async Task RenderPartialMethods_CallHtmlHelperWithExpectedArguments(
        Func<IHtmlHelper, Task> renderPartialAsyncMethod,
        object expectedModel,
        ViewDataDictionary expectedViewData)
    {
        // Arrange
        var htmlContent = Mock.Of<IHtmlContent>();
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        if (expectedModel == null)
        {
            // Extension methods without model parameter use ViewData.Model to get Model.
            var viewData = expectedViewData ?? new ViewDataDictionary(new EmptyModelMetadataProvider());
            helper
                .SetupGet(h => h.ViewData)
                .Returns(viewData)
                .Verifiable();
        }

        helper
            .Setup(h => h.RenderPartialAsync("test", expectedModel, expectedViewData))
            .Returns(Task.FromResult(true))
            .Verifiable();

        // Act
        await renderPartialAsyncMethod(helper.Object);

        // Assert
        helper.VerifyAll();
    }

    // Func<IHtmlHelper, IHtmlContent>, expected Model, expected ViewDataDictionary
    public static TheoryData<Func<IHtmlHelper, Task>, object, ViewDataDictionary> RenderPartialAsyncExtensionMethods
    {
        get
        {
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var model = new object();
            return new TheoryData<Func<IHtmlHelper, Task>, object, ViewDataDictionary>
                {
                    { helper => helper.RenderPartialAsync("test"), null, null },
                    { helper => helper.RenderPartialAsync("test", model), model, null },
                    { helper => helper.RenderPartialAsync("test", viewData), null, viewData },
                };
        }
    }

    [Theory]
    [MemberData(nameof(RenderPartialAsyncExtensionMethods))]
    public async Task RenderPartialAsyncMethods_CallHtmlHelperWithExpectedArguments(
        Func<IHtmlHelper, Task> renderPartialAsyncMethod,
        object expectedModel,
        ViewDataDictionary expectedViewData)
    {
        // Arrange
        var htmlContent = Mock.Of<IHtmlContent>();
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        if (expectedModel == null)
        {
            // Extension methods without model parameter use ViewData.Model to get Model.
            var viewData = expectedViewData ?? new ViewDataDictionary(new EmptyModelMetadataProvider());
            helper
                .SetupGet(h => h.ViewData)
                .Returns(viewData)
                .Verifiable();
        }

        helper
            .Setup(h => h.RenderPartialAsync("test", expectedModel, expectedViewData))
            .Returns(Task.FromResult(true))
            .Verifiable();

        // Act
        await renderPartialAsyncMethod(helper.Object);

        // Assert
        helper.VerifyAll();
    }

    [Fact]
    public void Partial_InvokesPartialAsyncWithCurrentModel()
    {
        // Arrange
        var expected = new HtmlString("value");
        var model = new object();
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
        {
            Model = model
        };
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        helper.Setup(h => h.PartialAsync("test", model, null))
              .Returns(Task.FromResult((IHtmlContent)expected))
              .Verifiable();
        helper.SetupGet(h => h.ViewData)
              .Returns(viewData);

        // Act
        var actual = helper.Object.Partial("test");

        // Assert
        Assert.Same(expected, actual);
        helper.Verify();
    }

    [Fact]
    public void PartialWithModel_InvokesPartialAsyncWithPassedInModel()
    {
        // Arrange
        var expected = new HtmlString("value");
        var model = new object();
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        helper.Setup(h => h.PartialAsync("test", model, null))
              .Returns(Task.FromResult((IHtmlContent)expected))
              .Verifiable();

        // Act
        var actual = helper.Object.Partial("test", model);

        // Assert
        Assert.Same(expected, actual);
        helper.Verify();
    }

    [Fact]
    public void PartialWithViewData_InvokesPartialAsyncWithPassedInViewData()
    {
        // Arrange
        var expected = new HtmlString("value");
        var model = new object();
        var passedInViewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
        {
            Model = model
        };
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        helper.Setup(h => h.PartialAsync("test", model, passedInViewData))
              .Returns(Task.FromResult((IHtmlContent)expected))
              .Verifiable();
        helper.SetupGet(h => h.ViewData)
              .Returns(viewData);

        // Act
        var actual = helper.Object.Partial("test", passedInViewData);

        // Assert
        Assert.Same(expected, actual);
        helper.Verify();
    }

    [Fact]
    public void PartialWithViewDataAndModel_InvokesPartialAsyncWithPassedInViewDataAndModel()
    {
        // Arrange
        var expected = new HtmlString("value");
        var passedInModel = new object();
        var passedInViewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var helper = new Mock<IHtmlHelper>(MockBehavior.Strict);
        helper.Setup(h => h.PartialAsync("test", passedInModel, passedInViewData))
              .Returns(Task.FromResult((IHtmlContent)expected))
              .Verifiable();

        // Act
        var actual = helper.Object.Partial("test", passedInModel, passedInViewData);

        // Assert
        Assert.Same(expected, actual);
        helper.Verify();
    }

    [Fact]
    public void Partial_InvokesAndRendersPartialAsyncOnHtmlHelperOfT()
    {
        // Arrange
        var model = new TestModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var expected = DefaultTemplatesUtilities.FormatOutput(helper, model);

        // Act
        var actual = helper.Partial("some-partial");

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(actual));
    }

    [Fact]
    public void PartialWithModel_InvokesAndRendersPartialAsyncOnHtmlHelperOfT()
    {
        // Arrange
        var model = new TestModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper();
        var expected = DefaultTemplatesUtilities.FormatOutput(helper, model);

        // Act
        var actual = helper.Partial("some-partial", model);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(actual));
    }

    [Fact]
    public void PartialWithViewData_InvokesAndRendersPartialAsyncOnHtmlHelperOfT()
    {
        // Arrange
        var model = new TestModel();
        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
        var viewData = new ViewDataDictionary(helper.MetadataProvider);
        var expected = DefaultTemplatesUtilities.FormatOutput(helper, model);

        // Act
        var actual = helper.Partial("some-partial", viewData);

        // Assert
        Assert.Equal(expected, HtmlContentUtilities.HtmlContentToString(actual));
    }

    [Fact]
    public async Task PartialAsync_Throws_IfViewNotFound_MessageUsesGetViewLocations()
    {
        // Arrange
        var expected = "The partial view 'test-view' was not found. The following locations were searched:" +
            Environment.NewLine +
            "location1" + Environment.NewLine +
            "location2";

        var model = new TestModel();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", Enumerable.Empty<string>()))
            .Verifiable();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        var viewData = helper.ViewData;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.PartialAsync("test-view", model, viewData));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task PartialAsync_Throws_IfViewNotFound_MessageUsesFindViewLocations()
    {
        // Arrange
        var expected = "The partial view 'test-view' was not found. The following locations were searched:" +
            Environment.NewLine +
            "location1" + Environment.NewLine +
            "location2";

        var model = new TestModel();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location1", "location2" }))
            .Verifiable();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        var viewData = helper.ViewData;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.PartialAsync("test-view", model, viewData));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task PartialAsync_Throws_IfViewNotFound_MessageUsesAllLocations()
    {
        // Arrange
        var expected = "The partial view 'test-view' was not found. The following locations were searched:" +
            Environment.NewLine +
            "location1" + Environment.NewLine +
            "location2" + Environment.NewLine +
            "location3" + Environment.NewLine +
            "location4";

        var model = new TestModel();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location3", "location4" }))
            .Verifiable();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        var viewData = helper.ViewData;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.PartialAsync("test-view", model, viewData));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task RenderPartialAsync_Throws_IfViewNotFound_MessageUsesGetViewLocations()
    {
        // Arrange
        var expected = "The partial view 'test-view' was not found. The following locations were searched:" +
            Environment.NewLine +
            "location1" + Environment.NewLine +
            "location2";

        var model = new TestModel();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", Enumerable.Empty<string>()))
            .Verifiable();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        var viewData = helper.ViewData;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.RenderPartialAsync("test-view", model, viewData));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task RenderPartialAsync_Throws_IfViewNotFound_MessageUsesFindViewLocations()
    {
        // Arrange
        var expected = "The partial view 'test-view' was not found. The following locations were searched:" +
            Environment.NewLine +
            "location1" + Environment.NewLine +
            "location2";

        var model = new TestModel();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", Enumerable.Empty<string>()))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location1", "location2" }))
            .Verifiable();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        var viewData = helper.ViewData;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.RenderPartialAsync("test-view", model, viewData));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public async Task RenderPartialAsync_Throws_IfViewNotFound_MessageUsesAllLocations()
    {
        // Arrange
        var expected = "The partial view 'test-view' was not found. The following locations were searched:" +
            Environment.NewLine +
            "location1" + Environment.NewLine +
            "location2" + Environment.NewLine +
            "location3" + Environment.NewLine +
            "location4";

        var model = new TestModel();
        var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
        viewEngine
            .Setup(v => v.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location1", "location2" }))
            .Verifiable();
        viewEngine
            .Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ false))
            .Returns(ViewEngineResult.NotFound("test-view", new[] { "location3", "location4" }))
            .Verifiable();

        var helper = DefaultTemplatesUtilities.GetHtmlHelper(model, viewEngine.Object);
        var viewData = helper.ViewData;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => helper.RenderPartialAsync("test-view", model, viewData));
        Assert.Equal(expected, exception.Message);
    }

    private sealed class TestModel
    {
        public override string ToString()
        {
            return "test-model-content";
        }
    }
}
