// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web
{
    internal static class ProgressEventArgsReader
    {
        private static readonly JsonEncodedText LengthComputable = JsonEncodedText.Encode("lengthComputable");
        private static readonly JsonEncodedText Loaded = JsonEncodedText.Encode("loaded");
        private static readonly JsonEncodedText Total = JsonEncodedText.Encode("total");
        private static readonly JsonEncodedText Type = JsonEncodedText.Encode("type");

        internal static ProgressEventArgs Read(JsonElement jsonElement)
        {
            var eventArgs = new ProgressEventArgs();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.NameEquals(LengthComputable.EncodedUtf8Bytes))
                {
                    eventArgs.LengthComputable = property.Value.GetBoolean();
                }
                else if (property.NameEquals(Loaded.EncodedUtf8Bytes))
                {
                    eventArgs.Loaded = property.Value.GetInt64();
                }
                else if (property.NameEquals(Total.EncodedUtf8Bytes))
                {
                    eventArgs.Total = property.Value.GetInt64();
                }
                else if (property.NameEquals(Type.EncodedUtf8Bytes))
                {
                    eventArgs.Type = property.Value.GetString()!;
                }
            }
            return eventArgs;
        }
    }
}
