// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class EventCallbackFactoryTest
    {
        [Fact]
        public void Create_EventCallback_ReturnsInput()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)component.SomeAction;
            var input = new EventCallback(component, @delegate);

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, input);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_Action_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)component.SomeAction;

            // Act
            var callback = EventCallback.Factory.Create(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_Action_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)component.SomeAction;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_Action_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)(() => { });

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_Action_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(component, (Action)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_ActionT_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)component.SomeActionOfT;

            // Act
            var callback = EventCallback.Factory.Create(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_ActionT_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)component.SomeActionOfT;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_ActionT_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)((s) => { });

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_ActionT_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(component, (Action<string>)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTask_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<Task>)component.SomeFuncTask;

            // Act
            var callback = EventCallback.Factory.Create(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTask_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<Task>)component.SomeFuncTask;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTask_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<Task>)(() => Task.CompletedTask);

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTTask_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)component.SomeFuncTTask;

            // Act
            var callback = EventCallback.Factory.Create(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTask_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(component, (Func<Task>)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTTask_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)component.SomeFuncTTask;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTTask_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)((s) => Task.CompletedTask);

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void Create_FuncTTask_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create(component, (Func<string, Task>)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_EventCallback_ReturnsInput()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)component.SomeAction;
            var input = new EventCallback<string>(component, @delegate);

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, input);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_Action_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)component.SomeAction;

            // Act
            var callback = EventCallback.Factory.Create<string>(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_Action_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)component.SomeAction;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_Action_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action)(() => { });

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_Action_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(component, (Action)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_ActionT_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)component.SomeActionOfT;

            // Act
            var callback = EventCallback.Factory.Create<string>(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_ActionT_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)component.SomeActionOfT;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_ActionT_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(component, (Action<string>)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_ActionT_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)((s) => { });

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTask_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<Task>)component.SomeFuncTask;

            // Act
            var callback = EventCallback.Factory.Create<string>(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTask_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<Task>)component.SomeFuncTask;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTask_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<Task>)(() => Task.CompletedTask);

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTask_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(component, (Func<Task>)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTTask_AlreadyBoundToReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)component.SomeFuncTTask;

            // Act
            var callback = EventCallback.Factory.Create<string>(component, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTTask_DifferentReceiver()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)component.SomeFuncTTask;

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.False(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTTask_Unbound()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)((s) => Task.CompletedTask);

            var anotherComponent = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(anotherComponent, @delegate);

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(anotherComponent, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateT_FuncTTask_Null()
        {
            // Arrange
            var component = new EventComponent();

            // Act
            var callback = EventCallback.Factory.Create<string>(component, (Func<string, Task>)null);

            // Assert
            Assert.Null(callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateInferred_ActionT()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Action<string>)((s) => { });

            // Act
            var callback = EventCallback.Factory.CreateInferred<string>(component, @delegate, "hi");

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        [Fact]
        public void CreateInferred_FuncTTask()
        {
            // Arrange
            var component = new EventComponent();
            var @delegate = (Func<string, Task>)((s) => Task.CompletedTask);

            // Act
            var callback = EventCallback.Factory.CreateInferred<string>(component, @delegate, "hi");

            // Assert
            Assert.Same(@delegate, callback.Delegate);
            Assert.Same(component, callback.Receiver);
            Assert.True(callback.RequiresExplicitReceiver);
        }

        private class EventComponent : IComponent, IHandleEvent
        {
            public void SomeAction()
            {
            }

            public void SomeActionOfT(string e)
            {
            }

            public Task SomeFuncTask()
            {
                return Task.CompletedTask;
            }

            public Task SomeFuncTTask(string s)
            {
                return Task.CompletedTask;
            }

            public void Attach(RenderHandle renderHandle)
            {
                throw new NotImplementedException();
            }

            public Task HandleEventAsync(EventCallbackWorkItem item, object arg)
            {
                throw new NotImplementedException();
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                throw new NotImplementedException();
            }
        }
    }
}
