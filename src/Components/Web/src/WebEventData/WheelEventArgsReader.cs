// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class WheelEventArgsReader
{
    private static readonly JsonEncodedText s_deltaX = JsonEncodedText.Encode("deltaX");
    private static readonly JsonEncodedText s_deltaY = JsonEncodedText.Encode("deltaY");
    private static readonly JsonEncodedText s_deltaZ = JsonEncodedText.Encode("deltaZ");
    private static readonly JsonEncodedText s_deltaMode = JsonEncodedText.Encode("deltaMode");

    internal static WheelEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new WheelEventArgs();

        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_deltaX.EncodedUtf8Bytes))
            {
                eventArgs.DeltaX = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_deltaY.EncodedUtf8Bytes))
            {
                eventArgs.DeltaY = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_deltaZ.EncodedUtf8Bytes))
            {
                eventArgs.DeltaZ = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_deltaMode.EncodedUtf8Bytes))
            {
                eventArgs.DeltaMode = property.Value.GetInt64();
            }
            else
            {
                MouseEventArgsReader.ReadProperty(eventArgs, property);
            }
        }

        return eventArgs;
    }
}
