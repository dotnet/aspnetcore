// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

internal static class TouchEventArgsReader
{
    private static readonly JsonEncodedText s_detail = JsonEncodedText.Encode("detail");
    private static readonly JsonEncodedText s_clientX = JsonEncodedText.Encode("clientX");
    private static readonly JsonEncodedText s_clientY = JsonEncodedText.Encode("clientY");
    private static readonly JsonEncodedText s_pageX = JsonEncodedText.Encode("pageX");
    private static readonly JsonEncodedText s_pageY = JsonEncodedText.Encode("pageY");
    private static readonly JsonEncodedText s_screenX = JsonEncodedText.Encode("screenX");
    private static readonly JsonEncodedText s_screenY = JsonEncodedText.Encode("screenY");
    private static readonly JsonEncodedText s_ctrlKey = JsonEncodedText.Encode("ctrlKey");
    private static readonly JsonEncodedText s_shiftKey = JsonEncodedText.Encode("shiftKey");
    private static readonly JsonEncodedText s_altKey = JsonEncodedText.Encode("altKey");
    private static readonly JsonEncodedText s_metaKey = JsonEncodedText.Encode("metaKey");
    private static readonly JsonEncodedText s_type = JsonEncodedText.Encode("type");
    private static readonly JsonEncodedText s_identifier = JsonEncodedText.Encode("identifier");
    private static readonly JsonEncodedText s_changedTouches = JsonEncodedText.Encode("changedTouches");
    private static readonly JsonEncodedText s_targetTouches = JsonEncodedText.Encode("targetTouches");
    private static readonly JsonEncodedText s_touches = JsonEncodedText.Encode("touches");

    internal static TouchEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new TouchEventArgs();
        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(s_detail.EncodedUtf8Bytes))
            {
                eventArgs.Detail = property.Value.GetInt64();
            }
            else if (property.NameEquals(s_changedTouches.EncodedUtf8Bytes))
            {
                eventArgs.ChangedTouches = ReadTouchPointArray(property.Value);
            }
            else if (property.NameEquals(s_targetTouches.EncodedUtf8Bytes))
            {
                eventArgs.TargetTouches = ReadTouchPointArray(property.Value);
            }
            else if (property.NameEquals(s_touches.EncodedUtf8Bytes))
            {
                eventArgs.Touches = ReadTouchPointArray(property.Value);
            }
            else if (property.NameEquals(s_ctrlKey.EncodedUtf8Bytes))
            {
                eventArgs.CtrlKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_shiftKey.EncodedUtf8Bytes))
            {
                eventArgs.ShiftKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_altKey.EncodedUtf8Bytes))
            {
                eventArgs.AltKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_metaKey.EncodedUtf8Bytes))
            {
                eventArgs.MetaKey = property.Value.GetBoolean();
            }
            else if (property.NameEquals(s_type.EncodedUtf8Bytes))
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
            if (property.NameEquals(s_clientX.EncodedUtf8Bytes))
            {
                touchPoint.ClientX = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_clientY.EncodedUtf8Bytes))
            {
                touchPoint.ClientY = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_identifier.EncodedUtf8Bytes))
            {
                touchPoint.Identifier = property.Value.GetInt64();
            }
            else if (property.NameEquals(s_pageX.EncodedUtf8Bytes))
            {
                touchPoint.PageX = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_pageY.EncodedUtf8Bytes))
            {
                touchPoint.PageY = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_screenX.EncodedUtf8Bytes))
            {
                touchPoint.ScreenX = property.Value.GetDouble();
            }
            else if (property.NameEquals(s_screenY.EncodedUtf8Bytes))
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
