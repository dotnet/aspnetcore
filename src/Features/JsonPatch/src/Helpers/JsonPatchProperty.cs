// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch;

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
        ArgumentNullThrowHelper.ThrowIfNull(property);
        ArgumentNullThrowHelper.ThrowIfNull(parent);

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
