// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using System.Text.Json;

#nullable enable

namespace Microsoft.AspNetCore.Http.Json
{
    /// <summary>
    /// Options to configure JSON serialization settings for <see cref="HttpRequestJsonExtensions"/>
    /// and <see cref="HttpResponseJsonExtensions"/>.
    /// </summary>
    public class JsonOptions
    {
        internal static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            // Web defaults don't use the relex JSON escaping encoder.
            //
            // Because these options are for producing content that is written directly to the request
            // (and not embedded in an HTML page for example), we can use UnsafeRelaxedJsonEscaping.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Use a copy so the defaults are not modified.
        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(DefaultSerializerOptions);
    }
}
