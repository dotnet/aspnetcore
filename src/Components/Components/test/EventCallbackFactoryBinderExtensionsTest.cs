// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components;

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "not-an-integer!", });

        Assert.Equal(17, value); // Setter not called
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_IfConverterThrows_ConvertsEmptyStringToDefault()
    {
        // Arrange
        var value = 17;
        var component = new EventCountingComponent();
        Action<int> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = string.Empty, });

        Assert.Equal(0, value); // Calls setter to apply default value for this type
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
            return binder.InvokeAsync(new ChangeEventArgs() { Value = "18", });
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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "not-an-integer!", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue, });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = true, });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = true, });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42", });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_NullableEnum()
    {
        // Arrange
        var value = (AttributeTargets?)AttributeTargets.All;
        var component = new EventCountingComponent();
        Action<AttributeTargets?> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = AttributeTargets.Class;

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(), });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(CultureInfo.CurrentCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_NullableDateTime()
    {
        // Arrange
        var value = (DateTime?)DateTime.Now;
        var component = new EventCountingComponent();
        Action<DateTime?> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = new DateTime(2018, 3, 4, 1, 2, 3);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(CultureInfo.CurrentCulture), });

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
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(format, CultureInfo.InvariantCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_NullableDateTime_Format()
    {
        // Arrange
        var value = (DateTime?)DateTime.Now;
        var component = new EventCountingComponent();
        Action<DateTime?> setter = (_) => value = _;
        var format = "ddd yyyy-MM-dd";

        var binder = EventCallback.Factory.CreateBinder(component, setter, value, format);

        var expectedValue = new DateTime(2018, 3, 4);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(format, CultureInfo.InvariantCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_DateTimeOffset()
    {
        // Arrange
        var value = DateTimeOffset.Now;
        var component = new EventCountingComponent();
        Action<DateTimeOffset> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = new DateTime(2018, 3, 4, 1, 2, 3);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(CultureInfo.CurrentCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_NullableDateTimeOffset()
    {
        // Arrange
        var value = (DateTimeOffset?)DateTimeOffset.Now;
        var component = new EventCountingComponent();
        Action<DateTimeOffset?> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = new DateTime(2018, 3, 4, 1, 2, 3);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(CultureInfo.CurrentCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_DateTimeOffset_Format()
    {
        // Arrange
        var value = DateTimeOffset.Now;
        var component = new EventCountingComponent();
        Action<DateTimeOffset> setter = (_) => value = _;
        var format = "ddd yyyy-MM-dd";

        var binder = EventCallback.Factory.CreateBinder(component, setter, value, format);

        var expectedValue = new DateTime(2018, 3, 4);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(format, CultureInfo.InvariantCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_NullableDateTimeOffset_Format()
    {
        // Arrange
        var value = (DateTimeOffset?)DateTimeOffset.Now;
        var component = new EventCountingComponent();
        Action<DateTimeOffset?> setter = (_) => value = _;
        var format = "ddd yyyy-MM-dd";

        var binder = EventCallback.Factory.CreateBinder(component, setter, value, format);

        var expectedValue = new DateTime(2018, 3, 4);

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(format, CultureInfo.InvariantCulture), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    // This uses a type converter
    [Fact]
    public async Task CreateBinder_Guid()
    {
        // Arrange
        var value = Guid.NewGuid();
        var component = new EventCountingComponent();
        Action<Guid> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = Guid.NewGuid();

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    // This uses a type converter
    [Fact]
    public async Task CreateBinder_NullableGuid()
    {
        // Arrange
        var value = (Guid?)Guid.NewGuid();
        var component = new EventCountingComponent();
        Action<Guid?> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = Guid.NewGuid();

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(), });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public async Task CreateBinder_CustomTypeConverter()
    {
        // Arrange
        var value = new SecretMessage() { Message = "A message", };
        var component = new EventCountingComponent();
        Action<SecretMessage> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value);

        var expectedValue = new SecretMessage() { Message = "TypeConverter may be old, but it still works!", };

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = expectedValue.ToString(), });

        Assert.Equal(expectedValue.Message, value.Message);
        Assert.Equal(1, component.Count);
    }

    [Fact]
    public void CreateBinder_GenericWithoutTypeConverter_Throws()
    {
        var value = new ClassWithoutTypeConverter();
        var component = new EventCountingComponent();
        Action<ClassWithoutTypeConverter> setter = (_) => value = _;

        var ex = Assert.Throws<InvalidOperationException>(() => EventCallback.Factory.CreateBinder(component, setter, value));

        Assert.Equal(
            $"The type '{typeof(ClassWithoutTypeConverter).FullName}' does not have an associated TypeConverter that supports conversion from a string. " +
            $"Apply 'TypeConverterAttribute' to the type to register a converter.",
            ex.Message);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/30312")]
    [ReplaceCulture("fr-FR", "fr-FR")]
    public async Task CreateBinder_NumericType_WithCurrentCulture()
    {
        // Arrange
        var value = 17_000;
        var component = new EventCountingComponent();
        Action<int> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value, culture: null);

        var expectedValue = 42_000;

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42 000,00", });

        Assert.Equal(expectedValue, value);
        Assert.Equal(1, component.Count);
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/30312")]
    public async Task CreateBinder_NumericType_WithInvariantCulture()
    {
        // Arrange
        var value = 17_000;
        var component = new EventCountingComponent();
        Action<int> setter = (_) => value = _;

        var binder = EventCallback.Factory.CreateBinder(component, setter, value, CultureInfo.InvariantCulture);

        var expectedValue = 42_000;

        // Act
        await binder.InvokeAsync(new ChangeEventArgs() { Value = "42,000.00", });

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

        public void Attach(RenderHandle renderHandle)
        {
            throw new System.NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new System.NotImplementedException();
        }
    }

    private class ClassWithoutTypeConverter
    {
    }

    [TypeConverter(typeof(SecretMessageTypeConverter))]
    private class SecretMessage
    {
        public string Message { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }

    private class SecretMessageTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return false;
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string message)
            {
                return new SecretMessage() { Message = message, };
            }

            return null;
        }
    }
}
