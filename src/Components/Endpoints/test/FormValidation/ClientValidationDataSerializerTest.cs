// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

public class ClientValidationDataSerializerTest
{
    // Regression guard: the serializer must use Utf8JsonWriter's default HTML-safe encoder,
    // not UnsafeRelaxedJsonEscaping. The payload sits in the data-rules attribute of
    // <blazor-client-validation-data>; without escaping, hostile strings could break out of the
    // element or its attribute.
    [Fact]
    public void Serialize_EscapesHtmlSensitiveCharacters()
    {
        const string hostile = "<script>alert('&')</script></blazor-client-validation-data>";
        var descriptor = new ClientValidationFormDescriptor(new List<ClientValidationFieldDescriptor>
        {
            new(hostile, new List<ClientValidationRuleDescriptor>
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
