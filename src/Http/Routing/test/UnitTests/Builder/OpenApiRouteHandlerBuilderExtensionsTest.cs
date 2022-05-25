// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public void Produces_AddsProducesResponseTypeMetadataWithJsonContentType()
    {
        static void GenericProducesType(IEndpointConventionBuilder builder) =>
            builder.Produces(typeof(TestEndointConventionBuilder));
        static void SpecificProducesType(RouteHandlerBuilder builder) =>
            builder.Produces<TestEndointConventionBuilder>();

        static void AssertMetadata(EndpointBuilder builder)
        {
            var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));

            Assert.Equal(typeof(TestEndointConventionBuilder), metadata.Type);
            Assert.Equal(StatusCodes.Status200OK, metadata.StatusCode);
            Assert.Equal("application/json", Assert.Single(metadata.ContentTypes));
        }

        RunWithBothBuilders(GenericProducesType, SpecificProducesType, AssertMetadata);
    }

    [Fact]
    public void Produces_AddsProducesResponseTypeMetadataWithVoidType()
    {
        static void GenericProducesNotFound(IEndpointConventionBuilder builder) =>
            builder.Produces(statusCode: StatusCodes.Status404NotFound);
        static void SpecificProducesNotFound(RouteHandlerBuilder builder) =>
            builder.Produces(StatusCodes.Status404NotFound);

        static void AssertMetadata(EndpointBuilder builder)
        {
            var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));

            Assert.Equal(typeof(void), metadata.Type);
            Assert.Equal(StatusCodes.Status404NotFound, metadata.StatusCode);
            Assert.Empty(metadata.ContentTypes);
        }

        RunWithBothBuilders(GenericProducesNotFound, SpecificProducesNotFound, AssertMetadata);
    }

    [Fact]
    public void Produces_WithNoArgs_AddsProducesResponseTypeMetadata()
    {
        var builder = new TestEndointConventionBuilder();
        builder.Produces();

        var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));
        Assert.Equal(typeof(void), metadata.Type);
        Assert.Equal(StatusCodes.Status200OK, metadata.StatusCode);
        Assert.Empty(metadata.ContentTypes);
    }

    [Fact]
    public void ProdcesProblem_AddsProducesResponseTypeMetadataWithProblemDetailsType()
    {
        static void GenericProducesProblem(IEndpointConventionBuilder builder) =>
            builder.ProducesProblem(StatusCodes.Status400BadRequest);
        static void SpecificProducesProblem(RouteHandlerBuilder builder) =>
            builder.ProducesProblem(StatusCodes.Status400BadRequest);

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
    public void ProdcesValidiationProblem_AddsProducesResponseTypeMetadataWithHttpValidationProblemDetailsType()
    {
        static void GenericProducesValidationProblem(IEndpointConventionBuilder builder) =>
            builder.ProducesValidationProblem();
        static void SpecificProducesValidationProblem(RouteHandlerBuilder builder) =>
            builder.ProducesValidationProblem();

        static void AssertMetadata(EndpointBuilder builder)
        {
            var metadata = Assert.IsType<ProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));

            Assert.Equal(typeof(HttpValidationProblemDetails), metadata.Type);
            Assert.Equal(StatusCodes.Status400BadRequest, metadata.StatusCode);
            Assert.Equal("application/problem+json", Assert.Single(metadata.ContentTypes));
        }

        RunWithBothBuilders(GenericProducesValidationProblem, SpecificProducesValidationProblem, AssertMetadata);
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
    public void Accepts_AddsAcceptsMetadataWithSpecifiedType()
    {
        static void GenericAccepts(IEndpointConventionBuilder builder) =>
            builder.Accepts(typeof(TestEndointConventionBuilder), "text/plain");
        static void SpecificAccepts(RouteHandlerBuilder builder) =>
            builder.Accepts<TestEndointConventionBuilder>("text/plain");

        static void AssertMetadata(EndpointBuilder builder)
        {
            var metadata = Assert.IsType<AcceptsMetadata>(Assert.Single(builder.Metadata));

            Assert.Equal(typeof(TestEndointConventionBuilder), metadata.RequestType);
            Assert.Equal("text/plain", Assert.Single(metadata.ContentTypes));
            Assert.False(metadata.IsOptional);
        }

        RunWithBothBuilders(GenericAccepts, SpecificAccepts, AssertMetadata);
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
