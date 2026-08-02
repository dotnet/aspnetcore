// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder;

public class RoutingEndpointConventionBuilderExtensionsTest
{
    [Fact]
    public void RequireHost_RegisterConventionAndHostNames()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RequireHost("contoso.com:8080");

        // Assert
        var convention = Assert.Single(builder.Conventions);

        var endpointModel = new RouteEndpointBuilder((context) => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
        convention(endpointModel);

        var hostMetadata = Assert.IsType<HostAttribute>(Assert.Single(endpointModel.Metadata));

        Assert.Equal("contoso.com:8080", hostMetadata.Hosts.Single());
    }

    [Fact]
    public void RequireHost_AddsHostMetadata()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.RequireHost("www.example.com", "example.com");

        // Assert
        var endpoint = builder.Build();

        var metadata = endpoint.Metadata.GetMetadata<IHostMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal(new[] { "www.example.com", "example.com" }, metadata.Hosts);
    }

    [Fact]
    public void RequireHost_ChainedCall_ReturnedBuilderIsDerivedType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var chainedBuilder = builder.RequireHost("test");

        // Assert
        Assert.True(chainedBuilder.TestProperty);
    }

    [Fact]
    public void WithDisplayName_String_SetsDisplayName()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithDisplayName("test");

        // Assert
        var endpoint = builder.Build();
        Assert.Equal("test", endpoint.DisplayName);
    }

    [Fact]
    public void WithDisplayName_ChainedCall_ReturnedBuilderIsDerivedType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var chainedBuilder = builder.WithDisplayName("test");

        // Assert
        Assert.True(chainedBuilder.TestProperty);
    }

    [Fact]
    public void WithDisplayName_Func_SetsDisplayName()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithDisplayName(b => "test");

        // Assert
        var endpoint = builder.Build();
        Assert.Equal("test", endpoint.DisplayName);
    }

    [Fact]
    public void WithMetadata_AddsMetadata()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithMetadata("test", new HostAttribute("www.example.com", "example.com"));

        // Assert
        var endpoint = builder.Build();

        var hosts = endpoint.Metadata.GetMetadata<IHostMetadata>();
        Assert.NotNull(hosts);
        Assert.Equal(new[] { "www.example.com", "example.com" }, hosts.Hosts);

        var @string = endpoint.Metadata.GetMetadata<string>();
        Assert.Equal("test", @string);
    }

    [Fact]
    public void WithMetadata_ChainedCall_ReturnedBuilderIsDerivedType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var chainedBuilder = builder.WithMetadata("test");

        // Assert
        Assert.True(chainedBuilder.TestProperty);
    }

    [Fact]
    public void WithName_SetsEndpointName()
    {
        // Arrange
        var name = "SomeEndpointName";
        var builder = CreateBuilder();

        // Act
        builder.WithName(name);

        // Assert
        var endpoint = builder.Build();

        var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>();
        Assert.Equal(name, endpointName.EndpointName);

        var routeName = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
        Assert.Equal(name, routeName.RouteName);
    }

    [Fact]
    public void WithGroupName_SetsEndpointGroupName()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithGroupName("SomeEndpointGroupName");

        // Assert
        var endpoint = builder.Build();

        var endpointGroupName = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>();
        Assert.Equal("SomeEndpointGroupName", endpointGroupName.EndpointGroupName);
    }

    [Fact]
    public void FinallyConventions_RunAfterOtherConventions()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithMetadata("test-metadata");
        builder.Finally(inner =>
        {
            if (inner.Metadata.Any(md => md is string smd && smd == "test-metadata"))
            {
                inner.Metadata.Add("found-previous-metadata");
            }
        });

        // Assert
        var endpoint = builder.Build();

        var metadata = endpoint.Metadata.OfType<string>().ToList();
        Assert.Contains("test-metadata", metadata);
        Assert.Contains("found-previous-metadata", metadata);
    }

    [Fact]
    public void FinallyConventions_CanExamineMetadataInFIFOOrder()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithMetadata("test-metadata");
        builder.Finally(inner =>
        {
            if (inner.Metadata.Any(md => md is string smd && smd == "test-metadata"))
            {
                inner.Metadata.Add("inner-metadata");
            }
        });
        builder.Finally(inner =>
        {
            if (inner.Metadata.Any(md => md is string smd && smd == "inner-metadata"))
            {
                inner.Metadata.Add("inner-metadata-2");
            }
        });

        // Assert
        var endpoint = builder.Build();

        var metadata = endpoint.Metadata.OfType<string>().ToArray();
        Assert.Equal(new[] { "test-metadata", "inner-metadata", "inner-metadata-2" }, metadata);
    }

    [Fact]
    public void WithOrder_Func_SetsOrder()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.WithOrder(47);

        // Assert
        var endpoint = builder.Build() as RouteEndpoint;
        Assert.Equal(47, endpoint.Order);
    }

    private TestEndpointConventionBuilder CreateBuilder()
    {
        var conventionBuilder = new DefaultEndpointConventionBuilder(new RouteEndpointBuilder(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse("/test"),
            order: 0));

        return new TestEndpointConventionBuilder(conventionBuilder);
    }

    private class TestEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly DefaultEndpointConventionBuilder _endpointConventionBuilder;
        public bool TestProperty { get; } = true;
        public IList<Action<EndpointBuilder>> Conventions { get; } = new List<Action<EndpointBuilder>>();

        public TestEndpointConventionBuilder(DefaultEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilder = endpointConventionBuilder;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            Conventions.Add(convention);
            _endpointConventionBuilder.Add(convention);
        }

        public void Finally(Action<EndpointBuilder> finalConvention)
        {
            _endpointConventionBuilder.Finally(finalConvention);
        }

        public Endpoint Build()
        {
            return _endpointConventionBuilder.Build();
        }
    }

    private class DefaultEndpointConventionBuilder : IEndpointConventionBuilder
    {
        internal EndpointBuilder EndpointBuilder { get; }

        private List<Action<EndpointBuilder>> _conventions;
        private List<Action<EndpointBuilder>> _finallyConventions;

        public DefaultEndpointConventionBuilder(EndpointBuilder endpointBuilder)
        {
            EndpointBuilder = endpointBuilder;
            _conventions = new();
            _finallyConventions = new();
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            var conventions = _conventions;

            if (conventions is null)
            {
                throw new InvalidOperationException(Resources.RouteEndpointDataSource_ConventionsCannotBeModifiedAfterBuild);
            }

            conventions.Add(convention);
        }

        public void Finally(Action<EndpointBuilder> finalConvention)
        {
            _finallyConventions?.Add(finalConvention);
        }

        public Endpoint Build()
        {
            // Only apply the conventions once
            var conventions = Interlocked.Exchange(ref _conventions, null);

            if (conventions is not null)
            {
                foreach (var convention in conventions)
                {
                    convention(EndpointBuilder);
                }
            }

            var finallyConventions = Interlocked.Exchange(ref _finallyConventions, null);
            if (finallyConventions is not null)
            {
                foreach (var finallyConvention in finallyConventions)
                {
                    finallyConvention(EndpointBuilder);
                }
            }

            return EndpointBuilder.Build();
        }
    }
}
