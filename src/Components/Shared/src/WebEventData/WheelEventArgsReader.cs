// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web
{
    internal static class WheelEventArgsReader
    {
        private static readonly JsonEncodedText DeltaX = JsonEncodedText.Encode("deltaX");
        private static readonly JsonEncodedText DeltaY = JsonEncodedText.Encode("deltaY");
        private static readonly JsonEncodedText DeltaZ = JsonEncodedText.Encode("deltaZ");
        private static readonly JsonEncodedText DeltaMode = JsonEncodedText.Encode("deltaMode");


        internal static WheelEventArgs Read(JsonElement jsonElement)
        {
            var eventArgs = new WheelEventArgs();

            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.NameEquals(DeltaX.EncodedUtf8Bytes))
                {
                    eventArgs.DeltaX = property.Value.GetDouble();
                }
                else if (property.NameEquals(DeltaY.EncodedUtf8Bytes))
                {
                    eventArgs.DeltaY = property.Value.GetDouble();
                }
                else if (property.NameEquals(DeltaZ.EncodedUtf8Bytes))
                {
                    eventArgs.DeltaZ = property.Value.GetDouble();
                }
                else if (property.NameEquals(DeltaMode.EncodedUtf8Bytes))
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
}
