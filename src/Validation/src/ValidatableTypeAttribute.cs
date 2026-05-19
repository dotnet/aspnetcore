// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Indicates that a type is validatable to support discovery by the
/// validations generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ValidatableTypeAttribute : Attribute
{
}
