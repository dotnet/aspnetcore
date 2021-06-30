// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web
{
    internal static class ChangeEventArgsReader
    {
        private static readonly JsonEncodedText ValueKey = JsonEncodedText.Encode("value");

        internal static ChangeEventArgs Read(JsonElement jsonElement)
        {
            var changeArgs = new ChangeEventArgs();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.NameEquals(ValueKey.EncodedUtf8Bytes))
                {
                    var value = property.Value;
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.Null:
                            break;
                        case JsonValueKind.String:
                            changeArgs.Value = value.GetString();
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            changeArgs.Value = value.GetBoolean();
                            break;
                        default:
                            throw new ArgumentException($"Unsupported {nameof(ChangeEventArgs)} value {jsonElement}.");
                    }
                    return changeArgs;
                }
            }

            return changeArgs;
        }
    }
}
