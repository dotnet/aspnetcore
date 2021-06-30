// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web
{
    internal static class PointerEventArgsReader
    {
        private static readonly JsonEncodedText PointerId = JsonEncodedText.Encode("pointerId");
        private static readonly JsonEncodedText Width = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText Height = JsonEncodedText.Encode("height");
        private static readonly JsonEncodedText Pressure = JsonEncodedText.Encode("pressure");
        private static readonly JsonEncodedText TiltX = JsonEncodedText.Encode("tiltX");
        private static readonly JsonEncodedText TiltY = JsonEncodedText.Encode("tiltY");
        private static readonly JsonEncodedText PointerType = JsonEncodedText.Encode("pointerType");
        private static readonly JsonEncodedText IsPrimary = JsonEncodedText.Encode("isPrimary");

        internal static PointerEventArgs Read(JsonElement jsonElement)
        {
            var eventArgs = new PointerEventArgs();
            MouseEventArgsReader.Read(jsonElement, eventArgs);

            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.NameEquals(PointerId.EncodedUtf8Bytes))
                {
                    eventArgs.PointerId = property.Value.GetInt64();
                }
                else if (property.NameEquals(Width.EncodedUtf8Bytes))
                {
                    eventArgs.Width = property.Value.GetSingle();
                }
                else if (property.NameEquals(Height.EncodedUtf8Bytes))
                {
                    eventArgs.Height = property.Value.GetSingle();
                }
                else if (property.NameEquals(Pressure.EncodedUtf8Bytes))
                {
                    eventArgs.Pressure = property.Value.GetSingle();
                }
                else if (property.NameEquals(TiltX.EncodedUtf8Bytes))
                {
                    eventArgs.TiltX = property.Value.GetSingle();
                }
                else if (property.NameEquals(TiltY.EncodedUtf8Bytes))
                {
                    eventArgs.TiltY = property.Value.GetSingle();
                }
                else if (property.NameEquals(PointerType.EncodedUtf8Bytes))
                {
                    eventArgs.PointerType = property.Value.GetString()!;
                }
                else if (property.NameEquals(IsPrimary.EncodedUtf8Bytes))
                {
                    eventArgs.IsPrimary = property.Value.GetBoolean();
                }
            }

            return eventArgs;
        }
    }
}
