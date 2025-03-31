// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Validation;

/// <summary>
/// Indicates that a type is validatable to support discovery by the
/// validations generator.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ValidatableTypeAttribute : Attribute
{
}
