// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateTypesWithAttribute()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.Run();

[ValidatableType]
public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;

    [Range(10, 100), Display(Name = "Valid identifier")]
    public int IntegerWithRangeAndDisplayName { get; set; } = 50;

    [Required]
    public SubType PropertyWithMemberAttributes { get; set; } = new SubType();

    public SubType PropertyWithoutMemberAttributes { get; set; } = new SubType();

    public SubTypeWithInheritance PropertyWithInheritance { get; set; } = new SubTypeWithInheritance();

    public List<SubType> ListOfSubTypes { get; set; } = [];

    [CustomValidation(ErrorMessage = "Value must be an even number")]
    public int IntegerWithCustomValidationAttribute { get; set; }

    [CustomValidation, Range(10, 100)]
    public int PropertyWithMultipleAttributes { get; set; } = 10;
}

public class CustomValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is int number && number % 2 == 0;
}

public class SubType
{
    [Required]
    public string RequiredProperty { get; set; } = "some-value";

    [StringLength(10)]
    public string? StringWithLength { get; set; }
}

public class SubTypeWithInheritance : SubType
{
    [EmailAddress]
    public string? EmailString { get; set; }
}
""";
        await Verify(source, out var compilation);
        VerifyValidatableType(compilation, "ComplexType", (validatableTypeInfo) =>
        {
            Assert.NotNull(validatableTypeInfo);
        });
    }
}
