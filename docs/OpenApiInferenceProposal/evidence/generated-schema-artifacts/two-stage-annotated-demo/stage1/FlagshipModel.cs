// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;

namespace GeneratedSchemaInspection;

[GenerateJsonSchema(PropertyNaming = NamingConvention.CamelCase, StrictConditionals = true)]
[Id("urn:flagship:conditional")]
[If(nameof(Kind), "business", 0)]
public sealed class FlagshipModel
{
    [Json.Schema.Generation.Required]
    public required string Kind { get; set; }

    [Json.Schema.Generation.Required]
    public required FlagshipAddress Address { get; set; }

    [Json.Schema.Generation.Required(ConditionGroup = 0)]
    public string? TaxId { get; set; }
}

[Id("urn:flagship:address")]
public sealed class FlagshipAddress
{
    [Json.Schema.Generation.Required]
    public required string Street { get; set; }

    [Json.Schema.Generation.Required]
    public required string City { get; set; }
}
