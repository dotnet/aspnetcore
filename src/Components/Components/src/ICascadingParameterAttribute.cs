// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

// Represents an attribute marking a parameter to be set by a cascading value.
// This exists so cascading parameter attributes can be defined outside the Components assembly.
// For example: [SupplyParameterFromForm].
internal interface ICascadingParameterAttribute
{
    public string? Name { get; set; }
}
