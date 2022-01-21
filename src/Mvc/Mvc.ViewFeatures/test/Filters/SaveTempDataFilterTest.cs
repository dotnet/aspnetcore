// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class SaveTempDataFilterTest
{
    public static TheoryData<IActionResult> ActionResultsData
    {
        get
        {
            return new TheoryData<IActionResult>()
                {
                    new TestActionResult(),
                    new TestKeepTempDataActionResult()
                };
        }
    }

    [Fact]
    public async Task OnResultExecuting_DoesntThrowIfResponseStarted()
    {
        // Arrange
        var responseFeature = new TestResponseFeature(hasStarted: true);
        var httpContext = GetHttpContext(responseFeature);
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Loose);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Verifiable();
        var filter = new SaveTempDataFilter(tempDataFactory.Object);
        var context = GetResultExecutingContext(httpContext);
        filter.OnResultExecuting(context);

        // Act
        // Checking it doesn't throw
        await responseFeature.FireOnSendingHeadersAsync();
    }

    [Fact]
    public void OnResourceExecuting_RegistersOnStartingCallback()
    {
        // Arrange
        var responseFeature = new Mock<IHttpResponseFeature>(MockBehavior.Strict);
        responseFeature
            .Setup(rf => rf.OnStarting(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
            .Verifiable();
        responseFeature
            .SetupGet(rf => rf.HasStarted)
            .Returns(false);

        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Verifiable();
        var filter = new SaveTempDataFilter(tempDataFactory.Object);
        var httpContext = GetHttpContext(responseFeature.Object);
        var context = GetResourceExecutingContext(httpContext);

        // Act
        filter.OnResourceExecuting(context);

        // Assert
        responseFeature.Verify();
        tempDataFactory.Verify(tdf => tdf.GetTempData(It.IsAny<HttpContext>()), Times.Never());
    }

    [Fact]
    public void OnResultExecuted_CanBeCalledTwice()
    {
        // Arrange
        var responseFeature = new TestResponseFeature();
        var httpContext = GetHttpContext(responseFeature);
        var tempData = GetTempDataDictionary();
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(tempData.Object)
            .Verifiable();
        var filter = new SaveTempDataFilter(tempDataFactory.Object);
        var context = GetResultExecutedContext(httpContext);

        // Act (No Assert)
        filter.OnResultExecuted(context);
        // Shouldn't have thrown
        filter.OnResultExecuted(context);
    }

    [Fact]
    public async Task OnResourceExecuting_DoesNotSaveTempData_WhenTempDataAlreadySaved()
    {
        // Arrange
        var responseFeature = new TestResponseFeature();
        var httpContext = GetHttpContext(responseFeature);
        httpContext.Items[SaveTempDataFilter.SaveTempDataFilterContextKey] = new SaveTempDataFilter.SaveTempDataContext() { TempDataSaved = true };
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Verifiable();
        var filter = new SaveTempDataFilter(tempDataFactory.Object);
        var context = GetResourceExecutingContext(httpContext);
        filter.OnResourceExecuting(context); // registers callback

        // Act
        await responseFeature.FireOnSendingHeadersAsync();

        // Assert
        tempDataFactory.Verify(tdf => tdf.GetTempData(It.IsAny<HttpContext>()), Times.Never());
    }

    [Fact]
    public async Task OnResourceExecuting_DoesNotSaveTempData_WhenUnhandledExceptionOccurs()
    {
        // Arrange
        var responseFeature = new TestResponseFeature();
        var httpContext = GetHttpContext(responseFeature);
        httpContext.Items[SaveTempDataFilter.SaveTempDataFilterContextKey] = new SaveTempDataFilter.SaveTempDataContext() { RequestHasUnhandledException = true };
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Verifiable();
        var filter = new SaveTempDataFilter(tempDataFactory.Object);
        var context = GetResourceExecutingContext(httpContext);
        filter.OnResourceExecuting(context); // registers callback

        // Act
        await responseFeature.FireOnSendingHeadersAsync();

        // Assert
        tempDataFactory.Verify(tdf => tdf.GetTempData(It.IsAny<HttpContext>()), Times.Never());
    }

    [Theory]
    [MemberData(nameof(ActionResultsData))]
    public async Task OnResultExecuting_SavesTempData_WhenTempData_NotSavedAlready(IActionResult result)
    {
        // Arrange
        var tempDataDictionary = GetTempDataDictionary();
        var filter = GetFilter(tempDataDictionary.Object);
        var responseFeature = new TestResponseFeature();
        var httpContext = GetHttpContext(responseFeature);
        var resourceContext = GetResourceExecutingContext(httpContext);
        var resultContext = GetResultExecutedContext(httpContext, result);

        filter.OnResourceExecuting(resourceContext); // registers callback
        filter.OnResultExecuted(resultContext);
        // Act
        await responseFeature.FireOnSendingHeadersAsync();

        // Assert
        tempDataDictionary.Verify(tdd => tdd.Save(), Times.Once());
    }

    [Fact]
    public async Task OnResourceExecuting_KeepsTempData_ForIKeepTempDataResult()
    {
        // Arrange
        var tempDataDictionary = GetTempDataDictionary();
        var filter = GetFilter(tempDataDictionary.Object);
        var responseFeature = new TestResponseFeature();
        var httpContext = GetHttpContext(responseFeature);
        var resourceContext = GetResourceExecutingContext(httpContext);
        var resultContext = GetResultExecutedContext(httpContext, new TestKeepTempDataActionResult());
        filter.OnResourceExecuting(resourceContext); // registers callback
        filter.OnResultExecuted(resultContext);

        // Act
        await responseFeature.FireOnSendingHeadersAsync();

        // Assert
        tempDataDictionary.Verify(tdf => tdf.Keep(), Times.Once());
        tempDataDictionary.Verify(tdf => tdf.Save(), Times.Once());
    }

    [Fact]
    public async Task OnResultExecuting_DoesNotKeepTempData_ForNonIKeepTempDataResult()
    {
        // Arrange
        var tempDataDictionary = GetTempDataDictionary();
        var filter = GetFilter(tempDataDictionary.Object);
        var responseFeature = new TestResponseFeature();
        var actionContext = GetHttpContext(responseFeature);
        var context = GetResourceExecutingContext(actionContext);
        filter.OnResourceExecuting(context); // registers callback

        // Act
        await responseFeature.FireOnSendingHeadersAsync();

        // Assert
        tempDataDictionary.Verify(tdf => tdf.Keep(), Times.Never());
        tempDataDictionary.Verify(tdf => tdf.Save(), Times.Once());
    }

    [Fact]
    public void OnResultExecuted_DoesNotSaveTempData_WhenResponseHas_AlreadyStarted()
    {
        // Arrange
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Verifiable();
        var filter = new SaveTempDataFilter(tempDataFactory.Object);
        var httpContext = GetHttpContext(new TestResponseFeature(hasStarted: true));
        var context = GetResultExecutedContext(httpContext);

        // Act
        filter.OnResultExecuted(context);

        // Assert
        tempDataFactory.Verify(tdf => tdf.GetTempData(It.IsAny<HttpContext>()), Times.Never());
    }

    [Theory]
    [MemberData(nameof(ActionResultsData))]
    public void OnResultExecuted_SavesTempData_WhenResponseHas_NotStarted(IActionResult result)
    {
        // Arrange
        var tempDataDictionary = GetTempDataDictionary();
        var filter = GetFilter(tempDataDictionary.Object);
        var context = GetResultExecutedContext(actionResult: result);

        // Act
        filter.OnResultExecuted(context);

        // Assert
        tempDataDictionary.Verify(tdf => tdf.Save(), Times.Once());
    }

    [Fact]
    public void OnResultExecuted_KeepsTempData_ForIKeepTempDataResult()
    {
        // Arrange
        var tempDataDictionary = GetTempDataDictionary();
        var filter = GetFilter(tempDataDictionary.Object);
        var context = GetResultExecutedContext(actionResult: new TestKeepTempDataActionResult());

        // Act
        filter.OnResultExecuted(context);

        // Assert
        tempDataDictionary.Verify(tdf => tdf.Keep(), Times.Once());
        tempDataDictionary.Verify(tdf => tdf.Save(), Times.Once());
    }

    [Fact]
    public void OnResultExecuted_DoesNotKeepTempData_ForNonIKeepTempDataResult()
    {
        // Arrange
        var tempDataDictionary = GetTempDataDictionary();
        var filter = GetFilter(tempDataDictionary.Object);
        var context = GetResultExecutedContext(actionResult: new TestActionResult());

        // Act
        filter.OnResultExecuted(context);

        // Assert
        tempDataDictionary.Verify(tdf => tdf.Keep(), Times.Never());
        tempDataDictionary.Verify(tdf => tdf.Save(), Times.Once());
    }

    private SaveTempDataFilter GetFilter(ITempDataDictionary tempDataDictionary)
    {
        var tempDataFactory = GetTempDataDictionaryFactory(tempDataDictionary);
        return new SaveTempDataFilter(tempDataFactory.Object);
    }

    private Mock<ITempDataDictionaryFactory> GetTempDataDictionaryFactory(ITempDataDictionary tempDataDictionary)
    {
        var tempDataFactory = new Mock<ITempDataDictionaryFactory>(MockBehavior.Strict);
        tempDataFactory
            .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
            .Returns(tempDataDictionary);
        return tempDataFactory;
    }

    private Mock<ITempDataDictionary> GetTempDataDictionary()
    {
        var tempDataDictionary = new Mock<ITempDataDictionary>(MockBehavior.Strict);
        tempDataDictionary
            .Setup(tdd => tdd.Keep())
            .Verifiable();
        tempDataDictionary
            .Setup(tdd => tdd.Save())
            .Verifiable();
        return tempDataDictionary;
    }

    private ResourceExecutingContext GetResourceExecutingContext(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            httpContext = GetHttpContext();
        }
        var actionResult = new TestActionResult();

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filters = new IFilterMetadata[] { };
        var valueProviderFactories = new IValueProviderFactory[] { };

        return new ResourceExecutingContext(actionContext, filters, valueProviderFactories);
    }

    private ResultExecutedContext GetResultExecutedContext(HttpContext httpContext = null, IActionResult actionResult = null)
    {
        if (httpContext == null)
        {
            httpContext = GetHttpContext();
        }
        if (actionResult == null)
        {
            actionResult = new TestActionResult();
        }
        return new ResultExecutedContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new IFilterMetadata[] { },
            actionResult,
            new TestController());
    }

    private ResultExecutingContext GetResultExecutingContext(ActionContext actionContext, IActionResult actionResult = null)
    {
        if (actionResult == null)
        {
            actionResult = new TestActionResult();
        }
        return new ResultExecutingContext(
            actionContext,
            new IFilterMetadata[] { },
            actionResult,
            new TestController());
    }

    private ResultExecutingContext GetResultExecutingContext(HttpContext httpContext = null, IActionResult actionResult = null)
    {
        if (httpContext == null)
        {
            httpContext = new DefaultHttpContext();
        }
        if (actionResult == null)
        {
            actionResult = new TestActionResult();
        }
        return new ResultExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new IFilterMetadata[] { },
            new Mock<IActionResult>().Object,
            new TestController());
    }

    private HttpContext GetHttpContext(IHttpResponseFeature responseFeature = null)
    {
        if (responseFeature == null)
        {
            responseFeature = new TestResponseFeature();
        }
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);
        return httpContext;
    }

    private ActionContext GetActionContext(HttpContext httpContext = null)
    {
        if (httpContext == null)
        {
            httpContext = GetHttpContext();
        }
        return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
    }

    private class TestController : Controller
    {
    }

    private class TestActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            return context.HttpContext.Response.WriteAsync($"Hello from {nameof(TestActionResult)}");
        }
    }

    private class TestKeepTempDataActionResult : IActionResult, IKeepTempDataResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            return context.HttpContext.Response.WriteAsync($"Hello from {nameof(TestKeepTempDataActionResult)}");
        }
    }

    private class TestResponseFeature : HttpResponseFeature
    {
        private bool _hasStarted;
        private Func<Task> _responseStartingAsync = () => Task.FromResult(true);

        public TestResponseFeature(bool hasStarted = false)
        {
            _hasStarted = hasStarted;
        }

        public override bool HasStarted
        {
            get
            {
                return _hasStarted;
            }
        }

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            if (_hasStarted)
            {
                throw new TimeZoneNotFoundException();
            }

            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await callback(state);
                await prior();
            };
        }

        public async Task FireOnSendingHeadersAsync()
        {
            await _responseStartingAsync();
            _hasStarted = true;
        }
    }
}
