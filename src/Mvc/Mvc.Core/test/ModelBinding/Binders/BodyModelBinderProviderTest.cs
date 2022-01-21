// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class BodyModelBinderProviderTest
{
    public static TheoryData<BindingSource> NonBodyBindingSources
    {
        get
        {
            return new TheoryData<BindingSource>()
                {
                    BindingSource.Header,
                    BindingSource.Form,
                    null,
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonBodyBindingSources))]
    public void GetBinder_WhenBindingSourceIsNotFromBody_ReturnsNull(BindingSource source)
    {
        // Arrange
        var provider = CreateProvider();

        var context = new TestModelBinderProviderContext(typeof(Person));
        context.BindingInfo.BindingSource = source;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBinder_WhenNoInputFormatters_Throws()
    {
        // Arrange
        var expected = $"'{typeof(MvcOptions).FullName}.{nameof(MvcOptions.InputFormatters)}' must not be empty. " +
            $"At least one '{typeof(IInputFormatter).FullName}' is required to bind from the body.";
        var provider = CreateProvider();
        var context = new TestModelBinderProviderContext(typeof(Person));
        context.BindingInfo.BindingSource = BindingSource.Body;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetBinder(context));
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public void GetBinder_WhenBindingSourceIsFromBody_ReturnsBinder()
    {
        // Arrange
        var provider = CreateProvider(new TestInputFormatter());
        var context = new TestModelBinderProviderContext(typeof(Person));
        context.BindingInfo.BindingSource = BindingSource.Body;

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<BodyModelBinder>(result);
    }

    [Fact]
    public void GetBinder_DoesNotThrowNullReferenceException()
    {
        // Arrange
        var context = new TestModelBinderProviderContext(typeof(Person));
        context.BindingInfo.BindingSource = BindingSource.Body;
        var formatter = new TestInputFormatter();
        var formatterList = new List<IInputFormatter> { formatter };
        var provider = new BodyModelBinderProvider(formatterList, new TestHttpRequestStreamReaderFactory());

        // Act & Assert (does not throw)
        provider.GetBinder(context);
    }

    [Fact]
    public void CalculateAllowEmptyBody_EmptyBodyBehaviorIsDefaultValue_UsesMvcOptions()
    {
        // Arrange
        var options = new MvcOptions { AllowEmptyInputInBodyModelBinding = true };

        // Act
        var allowEmpty = BodyModelBinderProvider.CalculateAllowEmptyBody(EmptyBodyBehavior.Default, options);

        // Assert
        Assert.True(allowEmpty);
    }

    [Fact]
    public void CalculateAllowEmptyBody_EmptyBodyBehaviorIsDefaultValue_DefaultsToFalseWhenOptionsIsUnavailable()
    {
        // Act
        var allowEmpty = BodyModelBinderProvider.CalculateAllowEmptyBody(EmptyBodyBehavior.Default, options: null);

        // Assert
        Assert.False(allowEmpty);
    }

    [Fact]
    public void CalculateAllowEmptyBody_EmptyBodyBehaviorIsAllow()
    {
        // Act
        var allowEmpty = BodyModelBinderProvider.CalculateAllowEmptyBody(EmptyBodyBehavior.Allow, options: new MvcOptions());

        // Assert
        Assert.True(allowEmpty);
    }

    [Fact]
    public void CalculateAllowEmptyBody_EmptyBodyBehaviorIsDisallowed()
    {
        // Arrange
        // MvcOptions.AllowEmptyInputInBodyModelBinding should be ignored if EmptyBodyBehavior disallows it
        var options = new MvcOptions { AllowEmptyInputInBodyModelBinding = true };

        // Act
        var allowEmpty = BodyModelBinderProvider.CalculateAllowEmptyBody(EmptyBodyBehavior.Disallow, options);

        // Assert
        Assert.False(allowEmpty);
    }

    private static BodyModelBinderProvider CreateProvider(params IInputFormatter[] formatters)
    {
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        return new BodyModelBinderProvider(
            new List<IInputFormatter>(formatters),
            new TestHttpRequestStreamReaderFactory(),
            loggerFactory);
    }

    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }

    private class TestInputFormatter : IInputFormatter
    {
        public bool CanRead(InputFormatterContext context)
        {
            throw new NotImplementedException();
        }

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            throw new NotImplementedException();
        }
    }
}
