// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CacheComponentJsonTest
{
    [Fact]
    public void AddHtml_AddsHtmlSegment()
    {
        var json = new CacheComponentJson();

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
        var json = new CacheComponentJson();

        json.AddHole(typeof(NotCacheComponent));

        Assert.Equal(1, json.Count);
        var segment = GetSegments(json)[0];
        Assert.Equal(CacheSegmentKind.Hole, segment.Kind);
        Assert.Equal(typeof(NotCacheComponent), segment.ComponentType);
        Assert.Null(segment.Html);
        Assert.Null(segment.RenderModeName);
        Assert.Null(segment.ComponentKey);
    }

    [Fact]
    public void AddHole_WithRenderModeAndKey()
    {
        var json = new CacheComponentJson();

        json.AddHole(typeof(NotCacheComponent), "InteractiveServer", "my-key");

        var segment = GetSegments(json)[0];
        Assert.Equal("InteractiveServer", segment.RenderModeName);
        Assert.Equal("my-key", segment.ComponentKey);
    }

    [Fact]
    public void AddHtml_ThrowsForNull()
    {
        var json = new CacheComponentJson();

        Assert.Throws<ArgumentNullException>(() => json.AddHtml(null!));
    }

    [Fact]
    public void AddHole_ThrowsForNullType()
    {
        var json = new CacheComponentJson();

        Assert.Throws<ArgumentNullException>(() => json.AddHole(null!));
    }

    [Fact]
    public void Count_ReflectsNumberOfSegments()
    {
        var json = new CacheComponentJson();
        Assert.Equal(0, json.Count);

        json.AddHtml("<p>1</p>");
        Assert.Equal(1, json.Count);

        json.AddHole(typeof(NotCacheComponent));
        Assert.Equal(2, json.Count);

        json.AddHtml("<p>2</p>");
        Assert.Equal(3, json.Count);
    }

    [Fact]
    public void SerializeDeserialize_HtmlOnly()
    {
        var original = new CacheComponentJson();
        original.AddHtml("<div>cached</div>");
        original.AddHtml("<p>more</p>");

        var serialized = original.Serialize();
        var restored = CacheComponentJson.Deserialize(serialized);

        Assert.Equal(2, restored.Count);
        var segments = GetSegments(restored);
        Assert.Equal(CacheSegmentKind.Html, segments[0].Kind);
        Assert.Equal("<div>cached</div>", segments[0].Html);
        Assert.Equal(CacheSegmentKind.Html, segments[1].Kind);
        Assert.Equal("<p>more</p>", segments[1].Html);
    }

    [Fact]
    public void SerializeDeserialize_HoleOnly()
    {
        var original = new CacheComponentJson();
        original.AddHole(typeof(NotCacheComponent));

        var serialized = original.Serialize();
        var restored = CacheComponentJson.Deserialize(serialized);

        Assert.Equal(1, restored.Count);
        var segment = GetSegments(restored)[0];
        Assert.Equal(CacheSegmentKind.Hole, segment.Kind);
        Assert.Equal(typeof(NotCacheComponent), segment.ComponentType);
    }

    [Fact]
    public void SerializeDeserialize_MixedSegments()
    {
        var original = new CacheComponentJson();
        original.AddHtml("<header>cached</header>");
        original.AddHole(typeof(NotCacheComponent));
        original.AddHtml("<footer>also cached</footer>");
        original.AddHole(typeof(CacheComponent), "InteractiveWebAssembly", "key-1");

        var serialized = original.Serialize();
        var restored = CacheComponentJson.Deserialize(serialized);

        Assert.Equal(4, restored.Count);
        var segments = GetSegments(restored);

        Assert.Equal(CacheSegmentKind.Html, segments[0].Kind);
        Assert.Equal("<header>cached</header>", segments[0].Html);

        Assert.Equal(CacheSegmentKind.Hole, segments[1].Kind);
        Assert.Equal(typeof(NotCacheComponent), segments[1].ComponentType);
        Assert.Null(segments[1].RenderModeName);
        Assert.Null(segments[1].ComponentKey);

        Assert.Equal(CacheSegmentKind.Html, segments[2].Kind);
        Assert.Equal("<footer>also cached</footer>", segments[2].Html);

        Assert.Equal(CacheSegmentKind.Hole, segments[3].Kind);
        Assert.Equal(typeof(CacheComponent), segments[3].ComponentType);
        Assert.Equal("InteractiveWebAssembly", segments[3].RenderModeName);
        Assert.Equal("key-1", segments[3].ComponentKey);
    }

    [Fact]
    public void SerializeDeserialize_EmptySegments()
    {
        var original = new CacheComponentJson();

        var serialized = original.Serialize();
        var restored = CacheComponentJson.Deserialize(serialized);

        Assert.Equal(0, restored.Count);
    }

    [Theory]
    [InlineData("InteractiveServer")]
    [InlineData("InteractiveWebAssembly")]
    [InlineData("InteractiveAuto")]
    public void SerializeDeserialize_PreservesRenderModes(string renderModeName)
    {
        var original = new CacheComponentJson();
        original.AddHole(typeof(NotCacheComponent), renderModeName);

        var serialized = original.Serialize();
        var restored = CacheComponentJson.Deserialize(serialized);

        var segment = GetSegments(restored)[0];
        Assert.Equal(renderModeName, segment.RenderModeName);
    }

    [Fact]
    public void SerializeDeserialize_PreservesHtmlWithSpecialCharacters()
    {
        var html = "<div class=\"test\" data-value='a&b'>Hello <em>world</em> &amp; goodbye</div>";
        var original = new CacheComponentJson();
        original.AddHtml(html);

        var serialized = original.Serialize();
        var restored = CacheComponentJson.Deserialize(serialized);

        Assert.Equal(html, GetSegments(restored)[0].Html);
    }

    [Fact]
    public void Deserialize_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => CacheComponentJson.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_ThrowsForInvalidJson()
    {
        Assert.ThrowsAny<Exception>(() => CacheComponentJson.Deserialize("not valid json"));
    }

    [Fact]
    public void Deserialize_ThrowsForUnknownSegmentType()
    {
        var json = """[{"Type":"unknown","Content":"test"}]""";

        var ex = Assert.Throws<InvalidOperationException>(() => CacheComponentJson.Deserialize(json));
        Assert.Contains("Unknown cache segment type", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsForHoleMissingComponentType()
    {
        var json = """[{"Type":"hole","Content":null}]""";

        var ex = Assert.Throws<InvalidOperationException>(() => CacheComponentJson.Deserialize(json));
        Assert.Contains("missing component type", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsForUnresolvableComponentType()
    {
        var json = """[{"Type":"hole","Content":"Some.Fake.Type, FakeAssembly"}]""";

        var ex = Assert.Throws<InvalidOperationException>(() => CacheComponentJson.Deserialize(json));
        Assert.Contains("Could not resolve hole component type", ex.Message);
    }

    [Fact]
    public void ReconstructRenderMode_ReturnsCorrectModes()
    {
        Assert.Null(CacheSegment.CreateHole(typeof(NotCacheComponent)).ReconstructRenderMode());
        Assert.IsType<InteractiveServerRenderMode>(CacheSegment.CreateHole(typeof(NotCacheComponent), "InteractiveServer").ReconstructRenderMode());
        Assert.IsType<InteractiveWebAssemblyRenderMode>(CacheSegment.CreateHole(typeof(NotCacheComponent), "InteractiveWebAssembly").ReconstructRenderMode());
        Assert.IsType<InteractiveAutoRenderMode>(CacheSegment.CreateHole(typeof(NotCacheComponent), "InteractiveAuto").ReconstructRenderMode());
    }

    [Fact]
    public void ReconstructRenderMode_ThrowsForUnknownMode()
    {
        var segment = CacheSegment.CreateHole(typeof(NotCacheComponent), "SomeFutureMode");

        var ex = Assert.Throws<InvalidOperationException>(() => segment.ReconstructRenderMode());
        Assert.Contains("Unknown cached render mode", ex.Message);
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

    [Fact]
    public void RenderMode_RoundTrips_ThroughNameAndReconstruct()
    {
        var modes = new IComponentRenderMode[] { RenderMode.InteractiveServer, RenderMode.InteractiveWebAssembly, RenderMode.InteractiveAuto };

        foreach (var mode in modes)
        {
            var name = CacheSegment.GetRenderModeName(mode);
            var segment = CacheSegment.CreateHole(typeof(NotCacheComponent), name);
            var reconstructed = segment.ReconstructRenderMode();

            Assert.Equal(mode.GetType(), reconstructed!.GetType());
        }
    }

    private static List<CacheSegment> GetSegments(CacheComponentJson json)
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
