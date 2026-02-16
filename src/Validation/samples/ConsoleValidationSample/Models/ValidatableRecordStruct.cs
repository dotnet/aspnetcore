// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace ConsoleValidationSample.Models;

[ValidatableType]
public record ValidatableRecordStruct(
    [Range(10, 100)]
    int IntegerWithRange,

    [Range(10, 100), Display(Name = "Custom Name")]
    int IntegerWithRangeAndDisplayName,

    SubRecordStruct SubProperty
);

public record struct SubRecordStruct([Required] string RequiredProperty, [StringLength(10)] string? StringWithLength);
