// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

[assembly: JsonSerializable(typeof(ClipboardEventArgs))]

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Supplies information about an clipboard event that is being raised.
    /// </summary>
    public class ClipboardEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string Type { get; set; } = default!;
    }

    /// <summary>
    /// 
    /// </summary>
    public static class WebEvent
    {
        private static readonly JsonSourceGeneration.JsonContext jsonContext = new (JsonSerializerOptionsProvider.Options);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static TEvent JsonDeserialize<[DynamicallyAccessedMembers(JsonSerialized)] TEvent>(string json)
        {
            return (TEvent)JsonSerializer.Deserialize(json, typeof(TEvent), jsonContext)!;
        }
    }
}
