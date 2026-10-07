// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;

namespace GeneratedSchemaInspection;

[GenerateJsonSchema(PropertyNaming = NamingConvention.CamelCase, StrictConditionals = true)]
[AdditionalProperties(false)]
[If(nameof(IsActive), true, "active")]
public sealed class FlagshipModel
{
    [Required]
    public bool IsActive { get; set; }

    [Required]
    public required string Email { get; set; }

    [MinLength(3, ConditionGroup = "active")]
    public string? DisplayName { get; set; }

    public FlagshipAddress? Address { get; set; }
}

public sealed class FlagshipAddress
{
    [Required]
    public required string Country { get; set; }
}
