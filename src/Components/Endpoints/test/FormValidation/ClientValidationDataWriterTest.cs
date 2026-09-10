// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

public class ClientValidationDataWriterTest
{
    // Regression guard: the writer must use Utf8JsonWriter's default HTML-safe encoder, not
    // UnsafeRelaxedJsonEscaping. The payload sits in the data-rules attribute of
    // <blazor-client-validation-data>; without escaping, hostile strings could break out of the
    // element or its attribute.
    [Fact]
    public void Complete_EscapesHtmlSensitiveCharacters()
    {
        const string hostile = "<script>alert('&')</script></blazor-client-validation-data>";

        var writer = new ClientValidationDataWriter();
        writer.BeginField(hostile);
        writer.BeginRule(hostile, hostile);
        writer.Param(hostile, hostile);
        writer.EndRule();
        writer.EndField();

        var json = writer.Complete();
        Assert.NotNull(json);

        // The raw payload must not contain unescaped HTML-sensitive characters that could break out
        // of the element or its attribute, and must carry the \u003C / \u003E escapes instead.
        Assert.DoesNotContain("<", json);
        Assert.DoesNotContain(">", json);
        Assert.DoesNotContain("'", json);
        Assert.DoesNotContain("</blazor-client-validation-data>", json);
        Assert.Contains("\\u003C", json); // escaped <
        Assert.Contains("\\u003E", json); // escaped >

        // The hostile string must still round-trip intact through every position it was written to,
        // proving the characters were escaped rather than dropped.
        using var document = JsonDocument.Parse(json!);
        var field = Assert.Single(document.RootElement.GetProperty("fields").EnumerateArray());
        Assert.Equal(hostile, field.GetProperty("name").GetString());

        var rule = Assert.Single(field.GetProperty("rules").EnumerateArray());
        Assert.Equal(hostile, rule.GetProperty("name").GetString());
        Assert.Equal(hostile, rule.GetProperty("message").GetString());

        var param = Assert.Single(rule.GetProperty("params").EnumerateObject());
        Assert.Equal(hostile, param.Name);
        Assert.Equal(hostile, param.Value.GetString());
    }

    [Fact]
    public void Complete_ReturnsNull_WhenNoFieldProducedRules()
    {
        var writer = new ClientValidationDataWriter();

        // A field is begun but never receives a rule, so nothing should be emitted.
        writer.BeginField("Name");
        writer.EndField();

        Assert.Null(writer.Complete());
    }
}
