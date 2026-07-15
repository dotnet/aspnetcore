// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

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
        Assert.DoesNotContain("<", json);
        Assert.DoesNotContain(">", json);
        Assert.DoesNotContain("'", json);
        Assert.DoesNotContain("</blazor-client-validation-data>", json);
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
