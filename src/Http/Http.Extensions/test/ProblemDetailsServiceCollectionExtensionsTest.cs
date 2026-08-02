// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public partial class ProblemDetailsServiceCollectionExtensionsTest
{
    [Fact]
    public void AddProblemDetails_AddsNeededServices()
    {
        // Arrange
        var collection = new ServiceCollection();

        // Act
        collection.AddProblemDetails();

        // Assert
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsService) && sd.ImplementationType == typeof(ProblemDetailsService));
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsWriter) && sd.ImplementationType == typeof(DefaultProblemDetailsWriter));
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IConfigureOptions<JsonOptions>) && sd.ImplementationType == typeof(ProblemDetailsJsonOptionsSetup));
    }

    [Fact]
    public void AddProblemDetails_DoesNotDuplicate_WhenMultipleCalls()
    {
        // Arrange
        var collection = new ServiceCollection();

        // Act
        collection.AddProblemDetails();
        collection.AddProblemDetails();

        // Assert
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsService) && sd.ImplementationType == typeof(ProblemDetailsService));
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsWriter) && sd.ImplementationType == typeof(DefaultProblemDetailsWriter));
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IConfigureOptions<JsonOptions>) && sd.ImplementationType == typeof(ProblemDetailsJsonOptionsSetup));
    }

    [Fact]
    public void AddProblemDetails_AllowMultipleWritersRegistration()
    {
        // Arrange
        var collection = new ServiceCollection();
        var expectedCount = 2;
        var mockWriter = Mock.Of<IProblemDetailsWriter>();
        collection.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IProblemDetailsWriter), mockWriter));

        // Act
        collection.AddProblemDetails();

        // Assert
        var serviceDescriptors = collection.Where(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IProblemDetailsWriter));
        Assert.True(
            (expectedCount == serviceDescriptors.Count()),
            $"Expected service type '{typeof(IProblemDetailsWriter)}' to be registered {expectedCount}" +
            $" time(s) but was actually registered {serviceDescriptors.Count()} time(s).");
    }

    [Fact]
    public void AddProblemDetails_KeepCustomRegisteredService()
    {
        // Arrange
        var collection = new ServiceCollection();
        var customService = Mock.Of<IProblemDetailsService>();
        collection.AddSingleton(typeof(IProblemDetailsService), customService);

        // Act
        collection.AddProblemDetails();

        // Assert
        var service = Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsService));
        Assert.Same(customService, service.ImplementationInstance);
    }

    [Fact]
    public void AddProblemDetails_CombinesProblemDetailsContext()
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddOptions<JsonOptions>();
        collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolver = new TestExtensionsJsonContext());

        // Act
        collection.AddProblemDetails();

        // Assert
        var services = collection.BuildServiceProvider();
        var jsonOptions = services.GetService<IOptions<JsonOptions>>();

        Assert.NotNull(jsonOptions.Value);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(ProblemDetails), jsonOptions.Value.SerializerOptions));
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(TypeA), jsonOptions.Value.SerializerOptions));
    }

    [Fact]
    public void AddProblemDetails_Throws_ForReadOnlyJsonOptions()
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddOptions<JsonOptions>();
        collection.ConfigureAll<JsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolver = new TestExtensionsJsonContext();
            options.SerializerOptions.MakeReadOnly();
        });

        // Act
        collection.AddProblemDetails();

        // Assert
        var services = collection.BuildServiceProvider();
        var jsonOptions = services.GetService<IOptions<JsonOptions>>();

        Assert.Throws<InvalidOperationException>(() => jsonOptions.Value);
    }

    public enum CustomContextBehavior
    {
        Prepend,
        Append,
        Replace,
    }

    [Theory]
    [InlineData(CustomContextBehavior.Prepend)]
    [InlineData(CustomContextBehavior.Append)]
    [InlineData(CustomContextBehavior.Replace)]
    public void AddProblemDetails_CombinesProblemDetailsContext_WhenAddingCustomContext(CustomContextBehavior behavior)
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddOptions<JsonOptions>();

        if (behavior == CustomContextBehavior.Prepend)
        {
            collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, TestExtensionsJsonContext.Default));
        }
        else if (behavior == CustomContextBehavior.Append)
        {
            collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolverChain.Add(TestExtensionsJsonContext.Default));
        }
        else
        {
            collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolver = TestExtensionsJsonContext.Default);
        }

        // Act
        collection.AddProblemDetails();

        // Assert
        var services = collection.BuildServiceProvider();
        var jsonOptions = services.GetService<IOptions<JsonOptions>>();

        Assert.NotNull(jsonOptions.Value);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(ProblemDetails), jsonOptions.Value.SerializerOptions));
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(TypeA), jsonOptions.Value.SerializerOptions));
    }

    [Fact]
    public void AddProblemDetails_CombinesProblemDetailsContext_EvenWhenNullTypeInfoResolver()
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddOptions<JsonOptions>();
        collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolver = null);

        // Act
        collection.AddProblemDetails();

        // Assert
        var services = collection.BuildServiceProvider();
        var jsonOptions = services.GetService<IOptions<JsonOptions>>();

        Assert.NotNull(jsonOptions.Value);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(ProblemDetails), jsonOptions.Value.SerializerOptions));
    }

    [Fact]
    public void AddProblemDetails_CombineProblemDetailsContext_WhenDefaultTypeInfoResolver()
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddOptions<JsonOptions>();
        collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver());

        // Act
        collection.AddProblemDetails();

        // Assert
        var services = collection.BuildServiceProvider();
        var jsonOptions = services.GetService<IOptions<JsonOptions>>();

        Assert.NotNull(jsonOptions.Value);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver);
        Assert.IsNotType<DefaultJsonTypeInfoResolver>(jsonOptions.Value.SerializerOptions.TypeInfoResolver);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(ProblemDetails), jsonOptions.Value.SerializerOptions));
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver.GetTypeInfo(typeof(TypeA), jsonOptions.Value.SerializerOptions));
    }

    [Fact]
    public void AddProblemDetails_CanHaveCustomJsonTypeInfo()
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddOptions<JsonOptions>();

        // Act
        collection.AddProblemDetails();

        // add any custom ProblemDetails TypeInfoResolvers after calling AddProblemDetails()
        var customProblemDetailsResolver = new CustomProblemDetailsTypeInfoResolver();
        collection.ConfigureAll<JsonOptions>(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, customProblemDetailsResolver));

        // Assert
        var services = collection.BuildServiceProvider();
        var jsonOptions = services.GetService<IOptions<JsonOptions>>();

        Assert.NotNull(jsonOptions.Value);
        Assert.NotNull(jsonOptions.Value.SerializerOptions.TypeInfoResolver);

        Assert.Equal(3, jsonOptions.Value.SerializerOptions.TypeInfoResolverChain.Count);
        Assert.IsType<CustomProblemDetailsTypeInfoResolver>(jsonOptions.Value.SerializerOptions.TypeInfoResolverChain[0]);
        Assert.Equal("Microsoft.AspNetCore.Http.ProblemDetailsJsonContext", jsonOptions.Value.SerializerOptions.TypeInfoResolverChain[1].GetType().FullName);
        Assert.IsType<DefaultJsonTypeInfoResolver>(jsonOptions.Value.SerializerOptions.TypeInfoResolverChain[2]);

        var pdTypeInfo = jsonOptions.Value.SerializerOptions.GetTypeInfo(typeof(ProblemDetails));
        Assert.Same(customProblemDetailsResolver.LastProblemDetailsInfo, pdTypeInfo);
    }

    [JsonSerializable(typeof(TypeA))]
    internal partial class TestExtensionsJsonContext : JsonSerializerContext
    { }

    public class TypeA { }

    internal class CustomProblemDetailsTypeInfoResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo LastProblemDetailsInfo { get; set; }

        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == typeof(ProblemDetails))
            {
                LastProblemDetailsInfo = JsonTypeInfo.CreateJsonTypeInfo<ProblemDetails>(options);
                return LastProblemDetailsInfo;
            }

            return null;
        }
    }
}
