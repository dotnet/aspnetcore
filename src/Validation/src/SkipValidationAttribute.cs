// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Indicates that a property, parameter, or a type should not be validated.
/// When applied to a property, validation is skipped for that property.
/// When applied to a parameter, validation is skipped for that parameter.
/// When applied to a type, validation is skipped for all properties and parameters of that type.
/// This includes skipping validation of nested properties for complex types.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class SkipValidationAttribute : Attribute
{
}
