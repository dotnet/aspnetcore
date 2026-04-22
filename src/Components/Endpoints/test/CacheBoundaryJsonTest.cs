// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheBoundaryJsonTest
{
    [Fact]
    public void AddHtml_AddsHtmlSegment()
    {
        var json = new CacheBoundaryJson();

        json.AddHtml("<p>hello</p>");

        Assert.Equal(1, json.Count);
        var segment = GetSegments(json)[0];
        Assert.Equal(CacheSegmentKind.Html, segment.Kind);
        Assert.Equal("<p>hello</p>", segment.Html);
        Assert.Null(segment.ComponentType);
    }

    [Fact]
    public void AddHole_AddsHoleSegment()
    {
        var json = new CacheBoundaryJson();

        json.AddHole(typeof(NotCacheBoundary));

        Assert.Equal(1, json.Count);
        var segment = GetSegments(json)[0];
        Assert.Equal(CacheSegmentKind.Hole, segment.Kind);
        Assert.Equal(typeof(NotCacheBoundary), segment.ComponentType);
        Assert.Null(segment.Html);
        Assert.Null(segment.RenderModeName);
        Assert.Null(segment.ComponentKey);
    }

    [Fact]
    public void AddHole_WithRenderModeAndKey()
    {
        var json = new CacheBoundaryJson();

        json.AddHole(typeof(NotCacheBoundary), "InteractiveServer", "my-key");

        var segment = GetSegments(json)[0];
        Assert.Equal("InteractiveServer", segment.RenderModeName);
        Assert.Equal("my-key", segment.ComponentKey);
    }

    [Fact]
    public void AddHtml_ThrowsForNull()
    {
        var json = new CacheBoundaryJson();

        Assert.Throws<ArgumentNullException>(() => json.AddHtml(null!));
    }

    [Fact]
    public void AddHole_ThrowsForNullType()
    {
        var json = new CacheBoundaryJson();

        Assert.Throws<ArgumentNullException>(() => json.AddHole(null!));
    }

    [Fact]
    public void SerializeDeserialize_HtmlOnly()
    {
        var original = new CacheBoundaryJson();
        original.AddHtml("<div>cached</div>");
        original.AddHtml("<p>more</p>");

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        Assert.Equal(2, restored.Count);
        var segments = GetSegments(restored);
        Assert.Equal(CacheSegmentKind.Html, segments[0].Kind);
        Assert.Equal("<div>cached</div>", segments[0].Html);
        Assert.Equal(CacheSegmentKind.Html, segments[1].Kind);
        Assert.Equal("<p>more</p>", segments[1].Html);
    }

    [Fact]
    public void SerializeDeserialize_MixedSegments()
    {
        var original = new CacheBoundaryJson();
        original.AddHtml("<header>cached</header>");
        original.AddHole(typeof(NotCacheBoundary));
        original.AddHtml("<footer>also cached</footer>");
        original.AddHole(typeof(CacheBoundary), "InteractiveWebAssembly", "key-1");

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        Assert.Equal(4, restored.Count);
        var segments = GetSegments(restored);

        Assert.Equal(CacheSegmentKind.Html, segments[0].Kind);
        Assert.Equal("<header>cached</header>", segments[0].Html);

        Assert.Equal(CacheSegmentKind.Hole, segments[1].Kind);
        Assert.Equal(typeof(NotCacheBoundary), segments[1].ComponentType);
        Assert.Null(segments[1].RenderModeName);
        Assert.Null(segments[1].ComponentKey);

        Assert.Equal(CacheSegmentKind.Html, segments[2].Kind);
        Assert.Equal("<footer>also cached</footer>", segments[2].Html);

        Assert.Equal(CacheSegmentKind.Hole, segments[3].Kind);
        Assert.Equal(typeof(CacheBoundary), segments[3].ComponentType);
        Assert.Equal("InteractiveWebAssembly", segments[3].RenderModeName);
        Assert.Equal("key-1", segments[3].ComponentKey);
    }

    [Fact]
    public void SerializeDeserialize_EmptySegments()
    {
        var original = new CacheBoundaryJson();

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        Assert.Equal(0, restored.Count);
    }

    [Fact]
    public void SerializeDeserialize_PreservesHtmlWithSpecialCharacters()
    {
        var html = "<div class=\"test\" data-value='a&b'>Hello <em>world</em> &amp; goodbye</div>";
        var original = new CacheBoundaryJson();
        original.AddHtml(html);

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        Assert.Equal(html, GetSegments(restored)[0].Html);
    }

    [Fact]
    public void SerializeDeserialize_PreservesIntKey()
    {
        var original = new CacheBoundaryJson();
        original.AddHole(typeof(NotCacheBoundary), componentKey: 42);

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        var segment = GetSegments(restored)[0];
        Assert.IsType<int>(segment.ComponentKey);
        Assert.Equal(42, segment.ComponentKey);
    }

    [Fact]
    public void SerializeDeserialize_PreservesGuidKey()
    {
        var guid = Guid.NewGuid();
        var original = new CacheBoundaryJson();
        original.AddHole(typeof(NotCacheBoundary), componentKey: guid);

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        var segment = GetSegments(restored)[0];
        Assert.IsType<Guid>(segment.ComponentKey);
        Assert.Equal(guid, segment.ComponentKey);
    }

    [Fact]
    public void SerializeDeserialize_PreservesStringKey()
    {
        var original = new CacheBoundaryJson();
        original.AddHole(typeof(NotCacheBoundary), componentKey: "my-key");

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        var segment = GetSegments(restored)[0];
        Assert.IsType<string>(segment.ComponentKey);
        Assert.Equal("my-key", segment.ComponentKey);
    }

    [Fact]
    public void SerializeDeserialize_PreservesNullKey()
    {
        var original = new CacheBoundaryJson();
        original.AddHole(typeof(NotCacheBoundary), componentKey: null);

        var serialized = original.Serialize();
        var restored = CacheBoundaryJson.Deserialize(serialized);

        var segment = GetSegments(restored)[0];
        Assert.Null(segment.ComponentKey);
    }

    [Fact]
    public void Deserialize_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => CacheBoundaryJson.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_ThrowsForInvalidJson()
    {
        Assert.ThrowsAny<Exception>(() => CacheBoundaryJson.Deserialize("not valid json"));
    }

    [Fact]
    public void Deserialize_ThrowsForUnknownSegmentType()
    {
        var json = """[{"Type":"unknown","Content":"test"}]""";

        var ex = Assert.Throws<InvalidOperationException>(() => CacheBoundaryJson.Deserialize(json));
        Assert.Contains("Unknown cache segment type", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsForHoleMissingComponentType()
    {
        var json = """[{"Type":"hole","Content":null}]""";

        var ex = Assert.Throws<InvalidOperationException>(() => CacheBoundaryJson.Deserialize(json));
        Assert.Contains("missing component type", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsForUnresolvableComponentType()
    {
        var json = """[{"Type":"hole","Content":"Some.Fake.Type, FakeAssembly"}]""";

        var ex = Assert.Throws<InvalidOperationException>(() => CacheBoundaryJson.Deserialize(json));
        Assert.Contains("Could not resolve hole component type", ex.Message);
    }

    [Fact]
    public void GetRenderModeName_ReturnsCorrectNames()
    {
        Assert.Null(CacheSegment.GetRenderModeName(null));
        Assert.Equal("InteractiveServer", CacheSegment.GetRenderModeName(RenderMode.InteractiveServer));
        Assert.Equal("InteractiveWebAssembly", CacheSegment.GetRenderModeName(RenderMode.InteractiveWebAssembly));
        Assert.Equal("InteractiveAuto", CacheSegment.GetRenderModeName(RenderMode.InteractiveAuto));
    }

    [Fact]
    public void GetRenderModeName_ThrowsForUnsupportedMode()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CacheSegment.GetRenderModeName(new TestRenderMode()));
        Assert.Contains("Unsupported render mode type", ex.Message);
    }

    private static List<CacheSegment> GetSegments(CacheBoundaryJson json)
    {
        var list = new List<CacheSegment>();
        foreach (var segment in json)
        {
            list.Add(segment);
        }
        return list;
    }

    private sealed class TestRenderMode : IComponentRenderMode;
}
