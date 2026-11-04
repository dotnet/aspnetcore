// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class TouchEventArgsReader
{
    private static readonly JsonEncodedText Detail = JsonEncodedText.Encode("detail");
    private static readonly JsonEncodedText ClientX = JsonEncodedText.Encode("clientX");
    private static readonly JsonEncodedText ClientY = JsonEncodedText.Encode("clientY");
    private static readonly JsonEncodedText PageX = JsonEncodedText.Encode("pageX");
    private static readonly JsonEncodedText PageY = JsonEncodedText.Encode("pageY");
    private static readonly JsonEncodedText ScreenX = JsonEncodedText.Encode("screenX");
    private static readonly JsonEncodedText ScreenY = JsonEncodedText.Encode("screenY");
    private static readonly JsonEncodedText CtrlKey = JsonEncodedText.Encode("ctrlKey");
    private static readonly JsonEncodedText ShiftKey = JsonEncodedText.Encode("shiftKey");
    private static readonly JsonEncodedText AltKey = JsonEncodedText.Encode("altKey");
    private static readonly JsonEncodedText MetaKey = JsonEncodedText.Encode("metaKey");
    private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");
    private static readonly JsonEncodedText Identifier = JsonEncodedText.Encode("identifier");
    private static readonly JsonEncodedText ChangedTouches = JsonEncodedText.Encode("changedTouches");
    private static readonly JsonEncodedText TargetTouches = JsonEncodedText.Encode("targetTouches");
    private static readonly JsonEncodedText Touches = JsonEncodedText.Encode("touches");

    internal static TouchEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new TouchEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(Detail.EncodedUtf8Bytes))
            {
                eventArgs.Detail = property.Value.GetInt64();
            }
            else if (property.NameEquals(ChangedTouches.EncodedUtf8Bytes))
            {
                eventArgs.ChangedTouches = ReadTouchPointArray(property.Value);
            }
            else if (property.NameEquals(TargetTouches.EncodedUtf8Bytes))
            {
                eventArgs.TargetTouches = ReadTouchPointArray(property.Value);
            }
            else if (property.NameEquals(Touches.EncodedUtf8Bytes))
            {
                eventArgs.Touches = ReadTouchPointArray(property.Value);
            }
            else if (property.NameEquals(CtrlKey.EncodedUtf8Bytes))
            {
                eventArgs.CtrlKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(ShiftKey.EncodedUtf8Bytes))
            {
                eventArgs.ShiftKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(AltKey.EncodedUtf8Bytes))
            {
                eventArgs.AltKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(MetaKey.EncodedUtf8Bytes))
            {
                eventArgs.MetaKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(Type.EncodedUtf8Bytes))
            {
                eventArgs.Type = property.Value.GetString()!;
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return eventArgs;
    }

    private static TouchPoint[] ReadTouchPointArray(JsonElement jsonElement)
    {
        var touchPoints = new TouchPoint[jsonElement.GetArrayLength()];
        var i = 0;
        foreach (var item in jsonElement.EnumerateArray())
        {
            touchPoints[i++] = ReadTouchPoint(item);
        }

        return touchPoints;
    }

    private static TouchPoint ReadTouchPoint(JsonElement jsonElement)
    {
        var touchPoint = new TouchPoint();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(ClientX.EncodedUtf8Bytes))
            {
                touchPoint.ClientX = property.Value.GetDouble();
            }
            else if (property.NameEquals(ClientY.EncodedUtf8Bytes))
            {
                touchPoint.ClientY = property.Value.GetDouble();
            }
            else if (property.NameEquals(Identifier.EncodedUtf8Bytes))
            {
                touchPoint.Identifier = property.Value.GetInt64();
            }
            else if (property.NameEquals(PageX.EncodedUtf8Bytes))
            {
                touchPoint.PageX = property.Value.GetDouble();
            }
            else if (property.NameEquals(PageY.EncodedUtf8Bytes))
            {
                touchPoint.PageY = property.Value.GetDouble();
            }
            else if (property.NameEquals(ScreenX.EncodedUtf8Bytes))
            {
                touchPoint.ScreenX = property.Value.GetDouble();
            }
            else if (property.NameEquals(ScreenY.EncodedUtf8Bytes))
            {
                touchPoint.ScreenY = property.Value.GetDouble();
            }
            else
            {
                throw new JsonException($"Unknown property {property.Name}");
            }
        }

        return touchPoint;
    }
}
