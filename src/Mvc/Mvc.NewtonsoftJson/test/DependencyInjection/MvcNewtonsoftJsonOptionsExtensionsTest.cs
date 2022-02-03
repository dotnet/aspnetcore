// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection;

public class MvcNewtonsoftJsonOptionsExtensionsTest
{
    [Fact]
    public void UseCamelCasing_WillSet_CamelCasingStrategy_NameStrategy()
    {
        // Arrange
        var options = new MvcNewtonsoftJsonOptions();
        options.SerializerSettings.ContractResolver = new DefaultContractResolver()
        {
            NamingStrategy = new DefaultNamingStrategy()
        };
        var expected = typeof(CamelCaseNamingStrategy);

        // Act
        options.UseCamelCasing(processDictionaryKeys: true);
        var resolver = options.SerializerSettings.ContractResolver as DefaultContractResolver;
        var actual = resolver.NamingStrategy;

        // Assert
        Assert.IsType(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_WillNot_OverrideSpecifiedNames()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseCamelCasing(processDictionaryKeys: true);

        var annotatedFoo = new AnnotatedFoo()
        {
            HelloWorld = "Hello"
        };
        var expected = "{\"HELLO-WORLD\":\"Hello\"}";

        // Act
        var actual = SerializeToJson(options, value: annotatedFoo);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_WillChange_PropertyNames()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseCamelCasing(processDictionaryKeys: true);
        var foo = new { TestName = "TestFoo", TestValue = 10 };
        var expected = "{\"testName\":\"TestFoo\",\"testValue\":10}";

        // Act
        var actual = SerializeToJson(options, value: foo);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_WillChangeFirstPartBeforeSeparator_InPropertyName()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseCamelCasing(processDictionaryKeys: true);
        var foo = new { TestFoo_TestValue = "Test" };
        var expected = "{\"testFoo_TestValue\":\"Test\"}";

        // Act
        var actual = SerializeToJson(options, value: foo);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_ProcessDictionaryKeys_WillChange_DictionaryKeys_IfTrue()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseCamelCasing(processDictionaryKeys: true);
        var dictionary = new Dictionary<string, int>
        {
            ["HelloWorld"] = 1,
            ["HELLOWORLD"] = 2
        };
        var expected = "{\"helloWorld\":1,\"helloworld\":2}";

        // Act
        var actual = SerializeToJson(options, value: dictionary);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_ProcessDictionaryKeys_WillChangeFirstPartBeforeSeparator_InDictionaryKey_IfTrue()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseCamelCasing(processDictionaryKeys: true);
        var dictionary = new Dictionary<string, int>()
        {
            ["HelloWorld_HelloWorld"] = 1
        };

        var expected = "{\"helloWorld_HelloWorld\":1}";

        // Act
        var actual = SerializeToJson(options, value: dictionary);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_ProcessDictionaryKeys_WillNotChangeDictionaryKeys_IfFalse()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseCamelCasing(processDictionaryKeys: false);
        var dictionary = new Dictionary<string, int>
        {
            ["HelloWorld"] = 1,
            ["HELLO-WORLD"] = 2
        };
        var expected = "{\"HelloWorld\":1,\"HELLO-WORLD\":2}";

        // Act
        var actual = SerializeToJson(options, value: dictionary);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseMemberCasing_WillNotChange_OverrideSpecifiedNames()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseMemberCasing();
        var annotatedFoo = new AnnotatedFoo()
        {
            HelloWorld = "Hello"
        };
        var expected = "{\"HELLO-WORLD\":\"Hello\"}";

        // Act
        var actual = SerializeToJson(options, value: annotatedFoo);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseMemberCasing_WillSet_DefaultNamingStrategy_AsNamingStrategy()
    {
        // Arrange
        var options = new MvcNewtonsoftJsonOptions();
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };
        var expected = typeof(DefaultNamingStrategy);

        // Act
        options.UseMemberCasing();
        var resolver = options.SerializerSettings.ContractResolver as DefaultContractResolver;
        var actual = resolver.NamingStrategy;

        // Assert
        Assert.IsType(expected, actual);
    }

    [Fact]
    public void UseMemberCasing_WillNotChange_PropertyNames()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseMemberCasing();
        var foo = new { fooName = "Test", FooValue = "Value" };
        var expected = "{\"fooName\":\"Test\",\"FooValue\":\"Value\"}";

        // Act
        var actual = SerializeToJson(options, value: foo);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseMemberCasing_WillNotChange_DictionaryKeys()
    {
        // Arrange
        var options = CreateDefaultMvcJsonOptions().UseMemberCasing();
        var dictionary = new Dictionary<string, int>()
        {
            ["HelloWorld"] = 1,
            ["helloWorld"] = 2,
            ["HELLO-WORLD"] = 3
        };
        var expected = "{\"HelloWorld\":1,\"helloWorld\":2,\"HELLO-WORLD\":3}";

        // Act
        var actual = SerializeToJson(options, value: dictionary);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UseCamelCasing_WillThrow_IfContractResolver_IsNot_DefaultContractResolver()
    {
        // Arrange
        var options = new MvcNewtonsoftJsonOptions();
        options.SerializerSettings.ContractResolver = new FooContractResolver();
        var expectedMessage = Resources.FormatInvalidContractResolverForJsonCasingConfiguration(nameof(FooContractResolver), nameof(DefaultContractResolver));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => options.UseCamelCasing(processDictionaryKeys: false));
        Assert.Equal(expectedMessage, actual: exception.Message);
    }

    [Fact]
    public void UseMemberCasing_WillThrow_IfContractResolver_IsNot_DefaultContractResolver()
    {
        // Arrange
        var options = new MvcNewtonsoftJsonOptions();
        options.SerializerSettings.ContractResolver = new FooContractResolver();
        var expectedMessage = Resources.FormatInvalidContractResolverForJsonCasingConfiguration(nameof(FooContractResolver), nameof(DefaultContractResolver));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => options.UseMemberCasing());
        Assert.Equal(expectedMessage, actual: exception.Message);
    }

    // NOTE: This method was created to make sure to create a different instance of contract resolver as by default
    // MvcJsonOptions uses a static shared instance of resolver which when changed causes other tests to fail.
    private MvcNewtonsoftJsonOptions CreateDefaultMvcJsonOptions()
    {
        var options = new MvcNewtonsoftJsonOptions();
        options.SerializerSettings.ContractResolver = JsonSerializerSettingsProvider.CreateContractResolver();
        return options;
    }

    private static string SerializeToJson(MvcNewtonsoftJsonOptions options, object value)
    {
        return JsonConvert.SerializeObject(
            value: value,
            formatting: Formatting.None,
            settings: options.SerializerSettings);
    }

    private class AnnotatedFoo
    {
        [JsonProperty("HELLO-WORLD")]
        public string HelloWorld { get; set; }
    }

    private class FooContractResolver : IContractResolver
    {
        public JsonContract ResolveContract(Type type)
        {
            throw new NotImplementedException();
        }
    }
}
