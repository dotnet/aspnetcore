// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc;

public class ApiConventionMethodAttributeTest
{
    [Fact]
    public void Constructor_ThrowsIfConventionMethodIsAnnotatedWithProducesAttribute()
    {
        // Arrange
        var methodName = typeof(ConventionWithProducesAttribute).FullName + '.' + nameof(ConventionWithProducesAttribute.Get);
        var attribute = typeof(ProducesAttribute);

        var expected = GetErrorMessage(methodName, attribute);

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new ApiConventionMethodAttribute(typeof(ConventionWithProducesAttribute), nameof(ConventionWithProducesAttribute.Get)),
            "conventionType",
            expected);
    }

    public static class ConventionWithProducesAttribute
    {
        [Produces(typeof(void))]
        public static void Get() { }
    }

    [Fact]
    public void Constructor_ThrowsIfTypeIsNotStatic()
    {
        // Arrange
        var methodName = typeof(ConventionWithProducesAttribute).FullName + '.' + nameof(ConventionWithProducesAttribute.Get);
        var attribute = typeof(ProducesAttribute);

        var expected = $"API convention type '{typeof(object)}' must be a static type.";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new ApiConventionMethodAttribute(typeof(object), nameof(object.ToString)),
            "conventionType",
            expected);
    }

    public static class ConventionWithNullableContextAttribute
    {
#nullable enable
        public static void Get(Foo? foo) { }
#nullable restore
    }

    public class Foo { }

    [Fact]
    public void Convention_With_NullableContextAttribute_Has_NullableContextAttribute_On_Get_Method()
    {
        // Arrange
        var type = typeof(ConventionWithNullableContextAttribute);
        var method = type.GetMethod(nameof(ConventionWithNullableContextAttribute.Get));
        var attributes = method.GetCustomAttributes(false);

        // Act & Assert
        Assert.Contains(attributes, (a) =>
        {
            var attributeType = a.GetType();
            return attributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute";
        });
    }

    [Fact]
    public void Convention_With_NullableContextAttribute_With_NullableContextAttribute_On_Get_Method_Does_Not_Throw()
    {
        // Arrange
        var methodName = typeof(ConventionWithNullableContextAttribute).FullName + '.' + nameof(ConventionWithNullableContextAttribute.Get);
        var attribute = typeof(ProducesAttribute);

        new ApiConventionMethodAttribute(typeof(ConventionWithNullableContextAttribute), nameof(ConventionWithNullableContextAttribute.Get));
    }

    [Fact]
    public void Constructor_ThrowsIfMethodCannotBeFound()
    {
        // Arrange
        var methodName = typeof(ConventionWithProducesAttribute).FullName + '.' + nameof(ConventionWithProducesAttribute.Get);
        var attribute = typeof(ProducesAttribute);
        var type = typeof(TestConventions);

        var expected = $"A method named 'DoesNotExist' was not found on convention type '{type}'.";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new ApiConventionMethodAttribute(typeof(TestConventions), "DoesNotExist"),
            "methodName",
            expected);
    }

    [Fact]
    public void Constructor_ThrowsIfMethodIsNotPublic()
    {
        // Arrange
        var methodName = typeof(ConventionWithProducesAttribute).FullName + '.' + nameof(ConventionWithProducesAttribute.Get);
        var attribute = typeof(ProducesAttribute);
        var type = typeof(TestConventions);

        var expected = $"A method named 'NotPublic' was not found on convention type '{type}'.";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new ApiConventionMethodAttribute(typeof(TestConventions), "NotPublic"),
            "methodName",
            expected);
    }

    [Fact]
    public void Constructor_ThrowsIfMethodIsAmbiguous()
    {
        // Arrange
        var methodName = typeof(ConventionWithProducesAttribute).FullName + '.' + nameof(ConventionWithProducesAttribute.Get);
        var attribute = typeof(ProducesAttribute);
        var type = typeof(TestConventions);

        var expected = $"Method name 'Method' is ambiguous for convention type '{type}'. More than one method found with the name 'Method'.";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new ApiConventionMethodAttribute(typeof(TestConventions), nameof(TestConventions.Method)),
            "methodName",
            expected);
    }

    private static class TestConventions
    {
        internal static void NotPublic() { }

        public static void Method(int value) { }

        public static void Method(string value) { }
    }

    private static string GetErrorMessage(string methodName, params Type[] attributes)
    {
        return $"Method {methodName} is decorated with the following attributes that are not allowed on an API convention method:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, attributes.Select(a => a.FullName)) +
            Environment.NewLine +
            $"The following attributes are allowed on API convention methods: {nameof(ProducesResponseTypeAttribute)}, {nameof(ProducesDefaultResponseTypeAttribute)}, {nameof(ApiConventionNameMatchAttribute)}";
    }
}
