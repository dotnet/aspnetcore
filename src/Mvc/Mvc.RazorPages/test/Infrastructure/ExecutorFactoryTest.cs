// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class ExecutorFactoryTest
{
    [Fact]
    public async Task CreateExecutor_ForActionResultMethod()
    {
        // Arrange
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = typeof(TestPage).GetMethod(nameof(TestPage.ActionResultReturningHandler)),
            Parameters = new HandlerParameterDescriptor[0],
        };

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(new TestPage(), null);
        var actionResult = await actionResultTask;
        Assert.IsType<EmptyResult>(actionResult);
    }

    [Fact]
    public async Task CreateExecutor_ForMethodReturningConcreteSubtypeOfIActionResult()
    {
        // Arrange
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = typeof(TestPage).GetMethod(nameof(TestPage.ConcreteActionResult)),
            Parameters = new HandlerParameterDescriptor[0],
        };

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(new TestPage(), null);
        var actionResult = await actionResultTask;
        Assert.IsType<ViewResult>(actionResult);
    }

    [Fact]
    public async Task CreateExecutor_ForActionResultReturningMethod_WithParameters()
    {
        // Arrange
        var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.ActionResultReturnHandlerWithParameters));
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = methodInfo,
            Parameters = CreateParameters(methodInfo),
        };

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(new TestPage(), CreateArguments(methodInfo));
        var actionResult = await actionResultTask;
        var contentResult = Assert.IsType<ContentResult>(actionResult);
        Assert.Equal("Hello 0", contentResult.Content);
    }

    [Fact]
    public async Task CreateExecutor_ForVoidReturningMethod()
    {
        // Arrange
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = typeof(TestPage).GetMethod(nameof(TestPage.VoidReturningHandler)),
            Parameters = new HandlerParameterDescriptor[0],
        };

        var page = new TestPage();

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(page, null);
        var actionResult = await actionResultTask;
        Assert.Null(actionResult);
        Assert.True(page.SideEffects);
    }

    [Fact]
    public async Task CreateExecutor_ForVoidTaskReturningMethod()
    {
        // Arrange
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = typeof(TestPage).GetMethod(nameof(TestPage.VoidTaskReturningHandler)),
            Parameters = new HandlerParameterDescriptor[0],
        };

        var page = new TestPage();

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(page, null);
        var actionResult = await actionResultTask;
        Assert.Null(actionResult);
        Assert.True(page.SideEffects);
    }

    [Fact]
    public async Task CreateExecutor_ForTaskOfIActionResultReturningMethod()
    {
        // Arrange
        var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.GenericTaskHandler));
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = methodInfo,
            Parameters = CreateParameters(methodInfo),
        };

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(new TestPage(), null);
        var actionResult = await actionResultTask;
        Assert.IsType<EmptyResult>(actionResult);
    }

    [Fact]
    public async Task CreateExecutor_ForTaskOfConcreteActionResultReturningMethod()
    {
        // Arrange
        var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.TaskReturningConcreteSubtype));
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = methodInfo,
            Parameters = CreateParameters(methodInfo),
        };

        // Act
        var executor = ExecutorFactory.CreateExecutor(handler);

        // Assert
        Assert.NotNull(executor);
        var actionResultTask = executor(new TestPage(), CreateArguments(methodInfo));
        var actionResult = await actionResultTask;
        var contentResult = Assert.IsType<ContentResult>(actionResult);
        Assert.Equal("value", contentResult.Content);
    }

    [Theory]
    [InlineData(nameof(TestPageModel.StringResult))]
    [InlineData(nameof(TestPageModel.TaskOfObject))]
    [InlineData(nameof(TestPageModel.ViewComponent))]
    public void CreateExecutor_ThrowsIfTypeIsNotAValidReturnType(string methodName)
    {
        // Arrange
        var methodInfo = typeof(TestPageModel).GetMethod(methodName);
        var handler = new HandlerMethodDescriptor()
        {
            MethodInfo = methodInfo,
            Parameters = CreateParameters(methodInfo),
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ExecutorFactory.CreateExecutor(handler));
        Assert.Equal($"Unsupported handler method return type '{methodInfo.ReturnType}'.", ex.Message);
    }

    private static object[] CreateArguments(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();

        return parameters.Select(s => GetDefaultValue(s)).ToArray();
    }

    private static object GetDefaultValue(ParameterInfo methodParameter)
    {
        object defaultValue = null;
        if (methodParameter.HasDefaultValue)
        {
            defaultValue = methodParameter.DefaultValue;
        }
        else if (methodParameter.ParameterType.GetTypeInfo().IsValueType)
        {
            defaultValue = Activator.CreateInstance(methodParameter.ParameterType);
        }

        return defaultValue;
    }

    private static HandlerParameterDescriptor[] CreateParameters(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();

        return parameters.Select(p => new HandlerParameterDescriptor()
        {
            BindingInfo = BindingInfo.GetBindingInfo(p.GetCustomAttributes()),
            Name = p.Name,
            ParameterInfo = p,
            ParameterType = p.ParameterType,
        }).ToArray();
    }

    private class TestPage : Page
    {
        public TestPage()
        {
        }

        public bool SideEffects { get; private set; }

        public IActionResult ActionResultReturningHandler() => new EmptyResult();

        public IActionResult ActionResultReturnHandlerWithParameters(int arg1, string arg2 = "Hello")
        {
            return new ContentResult
            {
                Content = $"{arg2} {arg1}",
            };
        }

        public ViewResult ConcreteActionResult() => new ViewResult();

        public void VoidReturningHandler()
        {
            SideEffects = true;
        }

        public async Task VoidTaskReturningHandler()
        {
            await Task.Run(() =>
            {
                SideEffects = true;
            });
        }

        public Task<IActionResult> GenericTaskHandler() => Task.FromResult<IActionResult>(new EmptyResult());

        public Task<ContentResult> TaskReturningConcreteSubtype(string arg = "value")
        {
            return Task.FromResult(new ContentResult
            {
                Content = arg,
            });
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class TestPageModel
    {
        public bool SideEffects { get; private set; }

        public IActionResult ActionResultReturningHandler() => new EmptyResult();

        public IActionResult ActionResultReturnHandlerWithParameters(int arg1, string arg2 = "Hello")
        {
            return new ContentResult
            {
                Content = $"{arg2} {arg1}",
            };
        }

        public ViewResult ConcreteActionResult() => new ViewResult();

        public void VoidReturningHandler()
        {
            SideEffects = true;
        }

        public async Task VoidTaskReturningHandler()
        {
            await Task.Run(() =>
            {
                SideEffects = true;
            });
        }

        public Task<IActionResult> GenericTaskHandler() => Task.FromResult<IActionResult>(new EmptyResult());

        public Task<ContentResult> TaskReturningConcreteSubtype(string arg = "value")
        {
            return Task.FromResult(new ContentResult
            {
                Content = arg,
            });
        }

        public string StringResult() => "";

        public Task<object> TaskOfObject() => Task.FromResult(new object());

        public IViewComponentResult ViewComponent() => new ViewViewComponentResult();
    }

    private class EmptyPage : Page
    {
        public EmptyPage()
        {
        }

        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
