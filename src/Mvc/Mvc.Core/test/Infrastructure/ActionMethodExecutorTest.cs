// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Internal;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ActionMethodExecutorTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesVoidActions(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.VoidAction));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("VoidResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.True(controller.Executed);
        Assert.IsType<EmptyResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturningIActionResult(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnIActionResult));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("SyncActionResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.True(valueTask.IsCompleted);
        Assert.IsType<ContentResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturningSubTypeOfActionResult(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsIActionResultSubType));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("SyncActionResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.IsType<ContentResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturningActionResultOfT(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsActionResultOfT));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        var result = Assert.IsType<ObjectResult>(valueTask.Result);

        Assert.Equal("SyncObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.NotNull(result.Value);
        Assert.IsType<TestModel>(result.Value);
        Assert.Equal(typeof(TestModel), result.DeclaredType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturningModelAsModel(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsModelAsModel));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        var result = Assert.IsType<ObjectResult>(valueTask.Result);

        Assert.Equal("SyncObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.NotNull(result.Value);
        Assert.IsType<TestModel>(result.Value);
        Assert.Equal(typeof(TestModel), result.DeclaredType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturningModelAsObject(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnModelAsObject));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        var result = Assert.IsType<ObjectResult>(valueTask.Result);

        Assert.Equal("SyncObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.NotNull(result.Value);
        Assert.IsType<TestModel>(result.Value);
        Assert.Equal(typeof(object), result.DeclaredType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturningActionResultAsObject(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsIActionResultSubType));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("SyncActionResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.IsType<ContentResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturnTask(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsTask));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("TaskResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.True(controller.Executed);
        Assert.IsType<EmptyResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsReturnAwaitable(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsAwaitable));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("AwaitableResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.True(controller.Executed);
        Assert.IsType<EmptyResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutorExecutesActionsAsynchronouslyReturningIActionResult(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnIActionResultAsync));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("TaskOfIActionResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.IsType<StatusCodeResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActionMethodExecutor_ExecutesActionsAsynchronouslyReturningActionResultSubType(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnActionResultAsync));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
                    new ActionContext(),
                    objectMethodExecutor,
                    mapper,
                    controller,
                    Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        await valueTask;
        Assert.Equal("TaskOfActionResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.IsType<ViewResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsAsynchronouslyReturningModel(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsModelAsModelAsync));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        var result = Assert.IsType<ObjectResult>(valueTask.Result);

        Assert.Equal("AwaitableObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.NotNull(result.Value);
        Assert.IsType<TestModel>(result.Value);
        Assert.Equal(typeof(TestModel), result.DeclaredType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsAsynchronouslyReturningModelAsObject(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsModelAsObjectAsync));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        var result = Assert.IsType<ObjectResult>(valueTask.Result);

        Assert.Equal("AwaitableObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.NotNull(result.Value);
        Assert.IsType<TestModel>(result.Value);
        Assert.Equal(typeof(object), result.DeclaredType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsAsynchronouslyReturningIActionResultAsObject(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnIActionResultAsObjectAsync));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        Assert.Equal("AwaitableObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.IsType<OkResult>(valueTask.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActionMethodExecutor_ExecutesActionsAsynchronouslyReturningActionResultOfT(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnActionResultOFTAsync));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act
        var valueTask = Execute(actionMethodExecutor, filterContext, withFilter);

        // Assert
        var result = Assert.IsType<ObjectResult>(valueTask.Result);

        Assert.Equal("AwaitableObjectResultExecutor", actionMethodExecutor.GetType().Name);
        Assert.NotNull(result.Value);
        Assert.IsType<TestModel>(result.Value);
        Assert.Equal(typeof(TestModel), result.DeclaredType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActionMethodExecutor_ThrowsIfIConvertFromIActionResult_ReturnsNull(bool withFilter)
    {
        // Arrange
        var mapper = new ActionResultTypeMapper();
        var controller = new TestController();
        var objectMethodExecutor = GetExecutor(nameof(TestController.ReturnsCustomConvertibleFromIActionResult));
        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
        var filterContext = new ControllerEndpointFilterInvocationContext(new Controllers.ControllerActionDescriptor(),
            new ActionContext(),
            objectMethodExecutor,
            mapper,
            controller,
            Array.Empty<object>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(actionMethodExecutor, filterContext, withFilter).AsTask());

        Assert.Equal($"Cannot return null from an action method with a return type of '{typeof(CustomConvertibleFromAction)}'.", ex.Message);
    }

    private async ValueTask<IActionResult> Execute(ActionMethodExecutor actionMethodExecutor,
                                                   ControllerEndpointFilterInvocationContext context,
                                                   bool withFilter)
    {
        if (withFilter)
        {
            return (IActionResult)await actionMethodExecutor.Execute(context);
        }
        return await actionMethodExecutor.Execute(context.ActionContext, context.Mapper, context.Executor, context.Controller, (object[])context.Arguments);
    }

    private static ObjectMethodExecutor GetExecutor(string methodName)
    {
        var type = typeof(TestController);
        var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(methodInfo);
        return ObjectMethodExecutor.Create(methodInfo, type.GetTypeInfo());
    }

    private class TestController
    {
        public bool Executed { get; set; }

        public void VoidAction() => Executed = true;

        public IActionResult ReturnIActionResult() => new ContentResult();

        public ContentResult ReturnsIActionResultSubType() => new ContentResult();

        public ActionResult<TestModel> ReturnsActionResultOfT() => new ActionResult<TestModel>(new TestModel());

        public CustomConvertibleFromAction ReturnsCustomConvertibleFromIActionResult() => new CustomConvertibleFromAction();

        public TestModel ReturnsModelAsModel() => new TestModel();

        public object ReturnModelAsObject() => new TestModel();

        public object ReturnIActionResultAsObject() => new RedirectResult("/foo");

        public Task ReturnsTask()
        {
            Executed = true;
            return Task.CompletedTask;
        }

        public YieldAwaitable ReturnsAwaitable()
        {
            Executed = true;
            return Task.Yield();
        }

        public Task<IActionResult> ReturnIActionResultAsync() => Task.FromResult((IActionResult)new StatusCodeResult(201));

        public Task<ViewResult> ReturnActionResultAsync() => Task.FromResult(new ViewResult { StatusCode = 200 });

        public Task<StatusCodeResult> ReturnsIActionResultSubTypeAsync() => Task.FromResult(new StatusCodeResult(200));

        public Task<TestModel> ReturnsModelAsModelAsync() => Task.FromResult(new TestModel());

        public Task<object> ReturnsModelAsObjectAsync() => Task.FromResult((object)new TestModel());

        public Task<object> ReturnIActionResultAsObjectAsync() => Task.FromResult((object)new OkResult());

        public Task<ActionResult<TestModel>> ReturnActionResultOFTAsync() => Task.FromResult(new ActionResult<TestModel>(new TestModel()));
    }

    private class TestModel
    {
    }

    private class CustomConvertibleFromAction : IConvertToActionResult
    {
        public IActionResult Convert() => null;
    }
}
