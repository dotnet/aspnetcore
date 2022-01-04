// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Routing;

// Tests LinkParser functionality using ParsePathByAddress - see tests for the extension
// methods for more E2E tests.
//
// Does not cover template processing in detail, those scenarios are validated by other tests.
public class DefaultLinkParserTest : LinkParserTestBase
{
    [Fact]
    public void ParsePathByAddresss_NoMatchingEndpoint_ReturnsNull()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", displayName: "Test1", metadata: new object[] { new IntMetadata(1), });

        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var parser = CreateLinkParser(services => { services.AddSingleton<ILoggerFactory>(loggerFactory); }, endpoint);

        // Act
        var values = parser.ParsePathByAddress(0, "/Home/Index/17");

        // Assert
        Assert.Null(values);

        Assert.Collection(
            sink.Writes,
            w => Assert.Equal("No endpoints found for address 0", w.Message));
    }

    [Fact]
    public void ParsePathByAddresss_HasMatches_ReturnsNullWhenParsingFails()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", displayName: "Test1", metadata: new object[] { new IntMetadata(1), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id2}", displayName: "Test2", metadata: new object[] { new IntMetadata(0), });

        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var parser = CreateLinkParser(services => { services.AddSingleton<ILoggerFactory>(loggerFactory); }, endpoint1, endpoint2);

        // Act
        var values = parser.ParsePathByAddress(0, "/");

        // Assert
        Assert.Null(values);

        Assert.Collection(
            sink.Writes,
            w => Assert.Equal("Found the endpoints Test2 for address 0", w.Message),
            w => Assert.Equal("Path parsing failed for endpoints Test2 and URI path /", w.Message));
    }

    [Fact]
    public void ParsePathByAddresss_HasMatches_ReturnsFirstSuccessfulParse()
    {
        // Arrange
        var endpoint0 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}", displayName: "Test1", metadata: new object[] { new IntMetadata(0), });
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", displayName: "Test2", metadata: new object[] { new IntMetadata(0), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id2}", displayName: "Test3", metadata: new object[] { new IntMetadata(0), });

        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var parser = CreateLinkParser(services => { services.AddSingleton<ILoggerFactory>(loggerFactory); }, endpoint0, endpoint1, endpoint2);

        // Act
        var values = parser.ParsePathByAddress(0, "/Home/Index/17");

        // Assert
        MatcherAssert.AssertRouteValuesEqual(new { controller = "Home", action = "Index", id = "17" }, values);

        Assert.Collection(
            sink.Writes,
            w => Assert.Equal("Found the endpoints Test1, Test2, Test3 for address 0", w.Message),
            w => Assert.Equal("Path parsing succeeded for endpoint Test2 and URI path /Home/Index/17", w.Message));
    }

    [Fact]
    public void ParsePathByAddresss_HasMatches_IncludesDefaults()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller=Home}/{action=Index}/{id?}", metadata: new object[] { new IntMetadata(0), });

        var parser = CreateLinkParser(endpoint);

        // Act
        var values = parser.ParsePathByAddress(0, "/");

        // Assert
        MatcherAssert.AssertRouteValuesEqual(new { controller = "Home", action = "Index", }, values);
    }

    [Fact]
    public void ParsePathByAddresss_HasMatches_RunsConstraints()
    {
        // Arrange
        var endpoint0 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id:int}", metadata: new object[] { new IntMetadata(0), });
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id2:alpha}", metadata: new object[] { new IntMetadata(0), });

        var parser = CreateLinkParser(endpoint0, endpoint1);

        // Act
        var values = parser.ParsePathByAddress(0, "/Home/Index/abc");

        // Assert
        MatcherAssert.AssertRouteValuesEqual(new { controller = "Home", action = "Index", id2 = "abc" }, values);
    }

    [Fact]
    public void GetRoutePatternMatcher_CanCache()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var dataSource = new DynamicEndpointDataSource(endpoint1);

        var parser = CreateLinkParser(dataSources: new[] { dataSource });

        var expected = parser.GetMatcherState(endpoint1);

        // Act
        var actual = parser.GetMatcherState(endpoint1);

        // Assert
        Assert.Same(expected.Matcher, actual.Matcher);
        Assert.Same(expected.Constraints, actual.Constraints);
    }

    [Fact]
    public void GetRoutePatternMatcherr_CanClearCache()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        var dataSource = new DynamicEndpointDataSource(endpoint1);

        var parser = CreateLinkParser(dataSources: new[] { dataSource });
        var original = parser.GetMatcherState(endpoint1);

        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new IntMetadata(1), });
        dataSource.AddEndpoint(endpoint2);

        // Act
        var actual = parser.GetMatcherState(endpoint1);

        // Assert
        Assert.NotSame(original.Matcher, actual.Matcher);
        Assert.NotSame(original.Constraints, actual.Constraints);
    }

    protected override void AddAdditionalServices(IServiceCollection services)
    {
        services.AddSingleton<IEndpointAddressScheme<int>, IntAddressScheme>();
    }

    private class IntAddressScheme : IEndpointAddressScheme<int>
    {
        private readonly EndpointDataSource _dataSource;

        public IntAddressScheme(EndpointDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public IEnumerable<Endpoint> FindEndpoints(int address)
        {
            return _dataSource.Endpoints.Where(e => e.Metadata.GetMetadata<IntMetadata>().Value == address);
        }
    }

    private class IntMetadata
    {
        public IntMetadata(int value)
        {
            Value = value;
        }
        public int Value { get; }
    }
}
