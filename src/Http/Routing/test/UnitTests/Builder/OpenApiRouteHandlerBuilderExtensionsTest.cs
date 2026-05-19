// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Routing.Builder;

public class OpenApiRouteHandlerBuilderExtensionsTest
{
    [Fact]
    public void ExcludeFromDescription_AddsExcludeFromDescriptionAttribute()
    {
        static void GenericExclude(IEndpointConventionBuilder builder) => builder.ExcludeFromDescription();
        static void SpecificExclude(RouteHandlerBuilder builder) => builder.ExcludeFromDescription();

        static void AssertMetadata(EndpointBuilder builder)
            => Assert.IsType<ExcludeFromDescriptionAttribute>(Assert.Single(builder.Metadata));

        RunWithBothBuilders(GenericExclude, SpecificExclude, AssertMetadata);
    }

    [Fact]
    public void WithTags_AddsTagsAttribute()
    {
        static void GenericWithTags(IEndpointConventionBuilder builder) => builder.WithTags("a", "b", "c");
        static void SpecificWithTags(RouteHandlerBuilder builder) => builder.WithTags("a", "b", "c");

        static void AssertMetadata(EndpointBuilder builder)
        {
            var tags = Assert.IsType<TagsAttribute>(Assert.Single(builder.Metadata));
            Assert.Collection(tags.Tags,
                tag => Assert.Equal("a", tag),
                tag => Assert.Equal("b", tag),
                tag => Assert.Equal("c", tag));
        }

        RunWithBothBuilders(GenericWithTags, SpecificWithTags, AssertMetadata);
    }

    [Fact]
    public void Produces_AddsProducesResponseTypeMetadataWithJsonContentType()
    {
        var testBuilder = new TestEndointConventionBuilder();
        var builder = new RouteHandlerBuilder(new[] { testBuilder });

        builder.Produces<TestEndointConventionBuilder>();

        var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(testBuilder.Metadata));
        Assert.Equal(typeof(TestEndointConventionBuilder), metadata.Type);
        Assert.Equal(StatusCodes.Status200OK, metadata.StatusCode);
        Assert.Equal("application/json", Assert.Single(metadata.ContentTypes));
    }

    [Fact]
    public void Produces_AddsProducesResponseTypeMetadataWithVoidType()
    {
        var testBuilder = new TestEndointConventionBuilder();
        var builder = new RouteHandlerBuilder(new[] { testBuilder });

        builder.Produces(StatusCodes.Status404NotFound);

        var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(testBuilder.Metadata));
        Assert.Equal(typeof(void), metadata.Type);
        Assert.Equal(StatusCodes.Status404NotFound, metadata.StatusCode);
        Assert.Empty(metadata.ContentTypes);
    }

    [Fact]
    public void ProducesProblem_AddsProducesResponseTypeMetadataWithProblemDetailsType()
    {
        static void GenericProducesProblem(IEndpointConventionBuilder builder) => builder.ProducesProblem(StatusCodes.Status400BadRequest);
        static void SpecificProducesProblem(RouteHandlerBuilder builder) => builder.ProducesProblem(StatusCodes.Status400BadRequest);

        static void AssertMetadata(EndpointBuilder builder)
        {
            var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));
            Assert.Equal(typeof(ProblemDetails), metadata.Type);
            Assert.Equal(StatusCodes.Status400BadRequest, metadata.StatusCode);
            Assert.Equal("application/problem+json", Assert.Single(metadata.ContentTypes));
        }

        RunWithBothBuilders(GenericProducesProblem, SpecificProducesProblem, AssertMetadata);

    }

    [Fact]
    public void ProducesValidationProblem_AddsProducesResponseTypeMetadataWithHttpValidationProblemDetailsType()
    {
        static void GenericProducesProblem(IEndpointConventionBuilder builder) => builder.ProducesValidationProblem();
        static void SpecificProducesProblem(RouteHandlerBuilder builder) => builder.ProducesValidationProblem();

        static void AssertMetadata(EndpointBuilder builder)
        {
            var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));
            Assert.Equal(typeof(HttpValidationProblemDetails), metadata.Type);
            Assert.Equal(StatusCodes.Status400BadRequest, metadata.StatusCode);
            Assert.Equal("application/problem+json", Assert.Single(metadata.ContentTypes));
        }

        RunWithBothBuilders(GenericProducesProblem, SpecificProducesProblem, AssertMetadata);
    }

    [Fact]
    public void Accepts_AddsAcceptsMetadataWithSpecifiedType()
    {
        var testBuilder = new TestEndointConventionBuilder();
        var builder = new RouteHandlerBuilder(new[] { testBuilder });

        builder.Accepts<TestEndointConventionBuilder>("text/plain");

        var metadata = Assert.IsType<AcceptsMetadata>(Assert.Single(testBuilder.Metadata));

        Assert.Equal(typeof(TestEndointConventionBuilder), metadata.RequestType);
        Assert.Equal("text/plain", Assert.Single(metadata.ContentTypes));
        Assert.False(metadata.IsOptional);
    }

    [Fact]
    public void WithDescription_AddsEndpointDescriptionAttribute()
    {
        var builder = new TestEndointConventionBuilder();
        builder.WithDescription("test description");

        var metadata = Assert.IsType<EndpointDescriptionAttribute>(Assert.Single(builder.Metadata));
        Assert.Equal("test description", metadata.Description);
    }

    [Fact]
    public void WithSummary_AddsEndpointSummaryAttribute()
    {
        var builder = new TestEndointConventionBuilder();
        builder.WithSummary("test summary");

        var metadata = Assert.IsType<EndpointSummaryAttribute>(Assert.Single(builder.Metadata));
        Assert.Equal("test summary", metadata.Summary);
    }

    private void RunWithBothBuilders(
        Action<IEndpointConventionBuilder> genericSetup,
        Action<RouteHandlerBuilder> specificSetup,
        Action<EndpointBuilder> assert)
    {
        var testBuilder = new TestEndointConventionBuilder();
        genericSetup(testBuilder);
        assert(testBuilder);

        var routeTestBuilder = new TestEndointConventionBuilder();
        var routeHandlerBuilder = new RouteHandlerBuilder(new[] { routeTestBuilder });
        specificSetup(routeHandlerBuilder);
        assert(routeTestBuilder);
    }

    private sealed class TestEndointConventionBuilder : EndpointBuilder, IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
            convention(this);
        }

        public override Endpoint Build() => throw new NotImplementedException();
    }
}
