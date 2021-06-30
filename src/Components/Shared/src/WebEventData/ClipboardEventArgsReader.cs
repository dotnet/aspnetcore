// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web
{
    internal static class ClipboardEventArgsReader
    {
        private static readonly JsonEncodedText TypeKey = JsonEncodedText.Encode("type");

        internal static ClipboardEventArgs Read(JsonElement jsonElement)
        {
            var eventArgs = new ClipboardEventArgs();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.NameEquals(TypeKey.EncodedUtf8Bytes))
                {
                    eventArgs.Type = property.Value.GetString()!;
                }
            }

            return eventArgs;
        }
    }
}
