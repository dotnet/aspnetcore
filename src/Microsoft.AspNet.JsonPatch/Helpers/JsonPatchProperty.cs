// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.JsonPatch
{
    /// <summary>
    /// Metadata for JsonProperty.
    /// </summary>
    public class JsonPatchProperty
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public JsonPatchProperty(JsonProperty property, object parent)
        {
            Property = property;
            Parent = parent;
        }

        /// <summary>
        /// Gets or sets JsonProperty.
        /// </summary>
        public JsonProperty Property { get; set; }

        /// <summary>
        /// Gets or sets Parent.
        /// </summary>
        public object Parent { get; set; }
    }
}