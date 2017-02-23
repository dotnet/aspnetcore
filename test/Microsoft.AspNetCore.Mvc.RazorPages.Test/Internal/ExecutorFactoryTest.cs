// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Test.Internal
{
    public class ExecutorFactoryTest
    {
        [Fact]
        public async Task CreateExecutor_ForActionResultMethod_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.ActionResultReturningHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new TestPage(), null);
            var actionResult = await actionResultTask;
            Assert.IsType<EmptyResult>(actionResult);
        }

        [Fact]
        public async Task CreateExecutor_ForMethodReturningConcreteSubtypeOfIActionResult_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.ConcreteActionResult));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new TestPage(), null);
            var actionResult = await actionResultTask;
            Assert.IsType<ViewResult>(actionResult);
        }

        [Fact]
        public async Task CreateExecutor_ForActionResultReturningMethod_WithParameters_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.ActionResultReturnHandlerWithParameters));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new TestPage(), null);
            var actionResult = await actionResultTask;
            var contentResult = Assert.IsType<ContentResult>(actionResult);
            Assert.Equal("Hello 0", contentResult.Content);
        }

        [Fact]
        public async Task CreateExecutor_ForVoidReturningMethod_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var page = new TestPage();
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.VoidReturningHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(page, null);
            var actionResult = await actionResultTask;
            Assert.Null(actionResult);
            Assert.True(page.SideEffects);
        }

        [Fact]
        public async Task CreateExecutor_ForVoidTaskReturningMethod_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var page = new TestPage();
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.VoidTaskReturningHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(page, null);
            var actionResult = await actionResultTask;
            Assert.Null(actionResult);
            Assert.True(page.SideEffects);
        }

        [Fact]
        public async Task CreateExecutor_ForTaskOfIActionResultReturningMethod_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.GenericTaskHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new TestPage(), null);
            var actionResult = await actionResultTask;
            Assert.IsType<EmptyResult>(actionResult);
        }

        [Fact]
        public async Task CreateExecutor_ForTaskOfConcreteActionResultReturningMethod_OnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPage).GetMethod(nameof(TestPage.TaskReturningConcreteSubtype));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new TestPage(), null);
            var actionResult = await actionResultTask;
            var contentResult = Assert.IsType<ContentResult>(actionResult);
            Assert.Equal("value", contentResult.Content);
        }

        [Fact]
        public async Task CreateExecutor_ForActionResultMethod_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.ActionResultReturningHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), new TestPageModel());
            var actionResult = await actionResultTask;
            Assert.IsType<EmptyResult>(actionResult);
        }

        [Fact]
        public async Task CreateExecutor_ForMethodReturningConcreteSubtypeOfIActionResult_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.ConcreteActionResult));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), new TestPageModel());
            var actionResult = await actionResultTask;
            Assert.IsType<ViewResult>(actionResult);
        }

        [Fact]
        public async Task CreateExecutor_ForActionResultReturningMethod_WithParameters_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.ActionResultReturnHandlerWithParameters));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), new TestPageModel());
            var actionResult = await actionResultTask;
            var contentResult = Assert.IsType<ContentResult>(actionResult);
            Assert.Equal("Hello 0", contentResult.Content);
        }

        [Fact]
        public async Task CreateExecutor_ForVoidReturningMethod_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var model = new TestPageModel();
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.VoidReturningHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), model);
            var actionResult = await actionResultTask;
            Assert.Null(actionResult);
            Assert.True(model.SideEffects);
        }

        [Fact]
        public async Task CreateExecutor_ForVoidTaskReturningMethod_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var model = new TestPageModel();
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.VoidTaskReturningHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), model);
            var actionResult = await actionResultTask;
            Assert.Null(actionResult);
            Assert.True(model.SideEffects);
        }

        [Fact]
        public async Task CreateExecutor_ForTaskOfIActionResultReturningMethod_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.GenericTaskHandler));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), new TestPageModel());
            var actionResult = await actionResultTask;
            Assert.IsType<EmptyResult>(actionResult);
        }

        [Fact]
        public async Task CreateExecutor_ForTaskOfConcreteActionResultReturningMethod_OnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPageModel).GetMethod(nameof(TestPageModel.TaskReturningConcreteSubtype));

            // Act
            var executor = ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo);

            // Assert
            Assert.NotNull(executor);
            var actionResultTask = executor(new EmptyPage(), new TestPageModel());
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
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModel).GetTypeInfo(),
            };
            var methodInfo = typeof(TestPageModel).GetMethod(methodName);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ExecutorFactory.CreateExecutor(actionDescriptor, methodInfo));
            Assert.Equal($"Unsupported handler method return type '{methodInfo.ReturnType}'.", ex.Message);
        }

        private class TestPage : Page
        {
            public TestPage()
            {
                Binder = new MockBinder();
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
                Binder = new MockBinder();
            }

            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class MockBinder : PageArgumentBinder
        {
            protected override Task<ModelBindingResult> BindAsync(PageContext context, object value, string name, Type type)
            {
                var result = ModelBindingResult.Failed();
                return Task.FromResult(result);
            }
        }
    }
}
