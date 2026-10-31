// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

public class TypeFilterAttributeTest
{
    [Fact]
    public void CreateService_TypeActivatesImplementationType()
    {
        // Arrange
        var value = "Some value";
        var uri = new Uri("http://www.asp.net");
        var serviceProvider = new ServiceCollection()
            .AddSingleton(value)
            .AddSingleton(uri)
            .BuildServiceProvider();

        var typeFilter = new TypeFilterAttribute(typeof(TestFilter));

        // Act
        var filter = typeFilter.CreateInstance(serviceProvider);

        // Assert
        var testFilter = Assert.IsType<TestFilter>(filter);
        Assert.Same(value, testFilter.Value);
        Assert.Same(uri, testFilter.Uri);
    }

    [Fact]
    public void CreateService_UsesArguments()
    {
        // Arrange
        var value = "Some value";
        var uri = new Uri("http://www.asp.net");
        var serviceProvider = new ServiceCollection()
            .AddSingleton("Value in DI")
            .AddSingleton(uri)
            .BuildServiceProvider();

        var typeFilter = new TypeFilterAttribute(typeof(TestFilter))
        {
            Arguments = new[] { value, }
        };

        // Act
        var filter = typeFilter.CreateInstance(serviceProvider);

        // Assert
        var testFilter = Assert.IsType<TestFilter>(filter);
        Assert.Same(value, testFilter.Value);
        Assert.Same(uri, testFilter.Uri);
    }

    [Fact]
    public void CreateService_UnwrapsFilterFactory()
    {
        // Arrange
        var value = "Some value";
        var uri = new Uri("http://www.asp.net");
        var serviceProvider = new ServiceCollection()
            .AddSingleton("Value in DI")
            .AddSingleton(uri)
            .BuildServiceProvider();

        var typeFilter = new TypeFilterAttribute(typeof(TestFilterFactory))
        {
            Arguments = new[] { value, }
        };

        // Act
        var filter = typeFilter.CreateInstance(serviceProvider);

        // Assert
        var testFilter = Assert.IsType<TestFilter>(filter);
        Assert.Same(value, testFilter.Value);
        Assert.Same(uri, testFilter.Uri);
    }

    public class TestFilter : IFilterMetadata
    {
        public TestFilter(string value, Uri uri)
        {
            Value = value;
            Uri = uri;
        }

        public string Value { get; }
        public Uri Uri { get; }
    }

    public class TestFilterFactory : IFilterFactory
    {
        private readonly string _value;
        private readonly Uri _uri;

        public TestFilterFactory(string value, Uri uri)
        {
            _value = value;
            _uri = uri;
        }

        public bool IsReusable => throw new NotImplementedException();

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new TestFilter(_value, _uri);
        }
    }
}
