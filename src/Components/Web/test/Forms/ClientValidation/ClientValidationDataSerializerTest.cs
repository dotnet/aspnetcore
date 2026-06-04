// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.Forms.ClientValidation;

namespace Microsoft.AspNetCore.Components.Forms;

public class ClientValidationDataSerializerTest
{
    // Regression guard: the serializer must use Utf8JsonWriter's default HTML-safe encoder,
    // not UnsafeRelaxedJsonEscaping. The payload sits as text inside <blazor-client-validation-data>;
    // without escaping, hostile strings could break out of the carrier element.
    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters()
    {
        const string hostile = "<script>alert('&')</script></blazor-client-validation-data>";
        var descriptor = new ClientValidationFormDescriptor(new List<ClientValidationFieldDescriptor>
        {
            new(hostile, new List<ClientValidationRule>
            {
                new(hostile, hostile, new Dictionary<string, string> { [hostile] = hostile }),
            }),
        });

        var json = ClientValidationDataSerializer.Serialize(descriptor);

        Assert.DoesNotContain("<", json);
        Assert.DoesNotContain(">", json);
        Assert.DoesNotContain("'", json);
        Assert.DoesNotContain("</blazor-client-validation-data>", json);
    }
}
