// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class EventCallbackFactoryBinderExtensionsTest
    {
        [Fact]
        public async Task CreateBinder_SwallowsConversionException()
        {
            // Arrange
            var value = 17;
            var component = new EventCountingComponent();
            Action<int> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "not-an-integer!", });

            Assert.Equal(17, value); // Setter not called
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_ThrowsSetterException()
        {
            // Arrange
            var component = new EventCountingComponent();
            Action<int> setter = (_) => { throw new InvalidTimeZoneException(); };

            var binder = EventCallback.Factory.CreateBinder(component, setter, 17);

            // Act
            await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
            {
                return binder.InvokeAsync(new UIChangeEventArgs() { Value = "18", });
            });

            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_BindsEmpty_DoesNotCallSetter()
        {
            // Arrange
            var value = 17;
            var component = new EventCountingComponent();
            Action<int> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "not-an-integer!", });

            Assert.Equal(17, value); // Setter not called
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_BindsEmpty_CallsSetterForNullable()
        {
            // Arrange
            var value = (int?)17;
            var component = new EventCountingComponent();
            Action<int?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "", });

            Assert.Null(value); // Setter called
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_String()
        {
            // Arrange
            var value = "hi";
            var component = new EventCountingComponent();
            Action<string> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = "bye";

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = expectedValue, });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Bool()
        {
            // Arrange
            var value = false;
            var component = new EventCountingComponent();
            Action<bool> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = true;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = true, });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_NullableBool()
        {
            // Arrange
            var value = (bool?)false;
            var component = new EventCountingComponent();
            Action<bool?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (bool?)true;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = true, });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Int()
        {
            // Arrange
            var value = 17;
            var component = new EventCountingComponent();
            Action<int> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = 42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_NullableInt()
        {
            // Arrange
            var value = (int?)17;
            var component = new EventCountingComponent();
            Action<int?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (int?)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Long()
        {
            // Arrange
            var value = (long)17;
            var component = new EventCountingComponent();
            Action<long> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (long)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_NullableLong()
        {
            // Arrange
            var value = (long?)17;
            var component = new EventCountingComponent();
            Action<long?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (long?)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Float()
        {
            // Arrange
            var value = (float)17;
            var component = new EventCountingComponent();
            Action<float> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (float)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_NullableFloat()
        {
            // Arrange
            var value = (float?)17;
            var component = new EventCountingComponent();
            Action<float?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (float?)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Double()
        {
            // Arrange
            var value = (double)17;
            var component = new EventCountingComponent();
            Action<double> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (double)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_NullableDouble()
        {
            // Arrange
            var value = (double?)17;
            var component = new EventCountingComponent();
            Action<double?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (double?)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Decimal()
        {
            // Arrange
            var value = (decimal)17;
            var component = new EventCountingComponent();
            Action<decimal> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (decimal)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_NullableDecimal()
        {
            // Arrange
            var value = (decimal?)17;
            var component = new EventCountingComponent();
            Action<decimal?> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = (decimal?)42;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = "42", });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_Enum()
        {
            // Arrange
            var value = AttributeTargets.All;
            var component = new EventCountingComponent();
            Action<AttributeTargets> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = AttributeTargets.Class;

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = expectedValue.ToString(), });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_DateTime()
        {
            // Arrange
            var value = DateTime.Now;
            var component = new EventCountingComponent();
            Action<DateTime> setter = (_) => value = _;

            var binder = EventCallback.Factory.CreateBinder(component, setter, value);

            var expectedValue = new DateTime(2018, 3, 4, 1, 2, 3);

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = expectedValue.ToString(), });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        [Fact]
        public async Task CreateBinder_DateTime_Format()
        {
            // Arrange
            var value = DateTime.Now;
            var component = new EventCountingComponent();
            Action<DateTime> setter = (_) => value = _;
            var format = "ddd yyyy-MM-dd";

            var binder = EventCallback.Factory.CreateBinder(component, setter, value, format);

            var expectedValue = new DateTime(2018, 3, 4);

            // Act
            await binder.InvokeAsync(new UIChangeEventArgs() { Value = expectedValue.ToString(format), });

            Assert.Equal(expectedValue, value);
            Assert.Equal(1, component.Count);
        }

        private class EventCountingComponent : IComponent, IHandleEvent
        {
            public int Count;

            public Task HandleEventAsync(EventCallbackWorkItem item, object arg)
            {
                Count++;
                return item.InvokeAsync(arg);
            }

            public void Configure(RenderHandle renderHandle)
            {
                throw new System.NotImplementedException();
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
